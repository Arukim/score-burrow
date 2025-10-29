using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.DataImport.Models;

namespace ScoreBurrow.DataImport.Services;

public class GameImporter
{
    private readonly ScoreBurrowDbContext _dbContext;
    private readonly CsvParser _csvParser;
    private readonly DateBacktracker _dateBacktracker;
    private readonly LeagueResolver _leagueResolver;
    private readonly PlayerResolver _playerResolver;
    private readonly RatingCalculator _ratingCalculator;

    public GameImporter(ScoreBurrowDbContext dbContext)
    {
        _dbContext = dbContext;
        _csvParser = new CsvParser();
        _dateBacktracker = new DateBacktracker();
        _leagueResolver = new LeagueResolver(dbContext);
        _playerResolver = new PlayerResolver(dbContext);
        _ratingCalculator = new RatingCalculator();
    }

    public async Task<ImportResult> ImportAsync(ImportOptions options)
    {
        Console.WriteLine("=== Starting Import Process ===\n");

        // 1. Parse CSV
        Console.WriteLine($"Parsing CSV file: {options.CsvPath}");
        var records = _csvParser.ParseCsv(options.CsvPath);
        Console.WriteLine($"Parsed {records.Count} CSV records\n");

        // 2. Group games by Red color
        Console.WriteLine("Grouping games by Red color boundary...");
        var gameGroups = _csvParser.GroupGamesByRedColor(records);
        Console.WriteLine($"Found {gameGroups.Count} games\n");

        // 3. Validate game groups
        _csvParser.ValidateGameGroups(gameGroups);

        // 4. Assign dates
        Console.WriteLine("Assigning game dates...");
        _dateBacktracker.AssignGameDates(gameGroups);
        Console.WriteLine($"Date range: {gameGroups.Min(g => g.GameDate):yyyy-MM-dd} to {gameGroups.Max(g => g.GameDate):yyyy-MM-dd}\n");

        // 5. Resolve league
        Console.WriteLine("Resolving league...");
        var league = await _leagueResolver.ResolveLeagueAsync(options);
        Console.WriteLine($"Using league: {league.Name} ({league.Id})\n");

        // 6. Resolve players
        Console.WriteLine("Resolving players...");
        var allPlayers = gameGroups.SelectMany(g => g.Participants.Select(p => p.Player)).Distinct();
        await _playerResolver.ResolvePlayersAsync(allPlayers, league.Id);
        Console.WriteLine($"Resolved {allPlayers.Count()} players\n");

        // 7. Load towns from database
        Console.WriteLine("Loading towns from database...");
        var towns = await _dbContext.Towns.ToDictionaryAsync(t => t.Name, StringComparer.OrdinalIgnoreCase);
        Console.WriteLine($"Loaded {towns.Count} towns\n");

        // 8. Preview or import
        var result = new ImportResult
        {
            TotalGames = gameGroups.Count,
            TotalParticipants = records.Count,
            LeagueName = league.Name,
            Players = allPlayers.ToList()
        };

        if (options.DryRun)
        {
            PrintDryRunSummary(gameGroups, league, result, towns);
            return result;
        }

        // 9. Import games
        Console.WriteLine("Importing games to database...");
        await ImportGamesAsync(gameGroups, league, towns);

        // 10. Update final ratings on LeagueMemberships
        Console.WriteLine("Updating current ratings on player memberships...");
        await UpdateFinalRatingsAsync(league.Id);

        Console.WriteLine($"\n✓ Successfully imported {result.TotalGames} games with {result.TotalParticipants} participants");
        Console.WriteLine("✓ Updated current ratings for all players");

        return result;
    }

    private async Task ImportGamesAsync(
        List<GameGroup> gameGroups, 
        League league, 
        Dictionary<string, Town> towns)
    {
        int importedGames = 0;
        int importedParticipants = 0;
        int ratingHistoryCount = 0;

        // Sort games by date to process in chronological order for accurate rating calculation
        var sortedGameGroups = gameGroups.OrderBy(g => g.GameDate).ToList();

        foreach (var gameGroup in sortedGameGroups)
        {
            // Create Game entity
            var game = new Game
            {
                Id = Guid.NewGuid(),
                LeagueId = league.Id,
                StartTime = gameGroup.GameDate,
                EndTime = gameGroup.GameDate.AddHours(2), // Assume 2-hour game duration
                MapName = gameGroup.MapName,
                Status = GameStatus.Completed,
                WinnerId = null // Will be set after participants are created
            };

            _dbContext.Games.Add(game);

            // Create GameParticipant entities
            var winner = gameGroup.Participants.FirstOrDefault(p => p.Result == 1);
            LeagueMembership? winnerMembership = null;
            var gameParticipants = new List<GameParticipant>();

            foreach (var participant in gameGroup.Participants)
            {
                var membership = _playerResolver.GetMembership(participant.Player);
                
                if (!towns.TryGetValue(participant.City, out var town))
                {
                    throw new InvalidOperationException($"Town not found: {participant.City}");
                }

                var playerColor = ParsePlayerColor(participant.Color);
                var isWinner = participant.Result == 1;
                var isTechnicalLoss = participant.IsTechnicalLoss == 1;

                var gameParticipant = new GameParticipant
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    LeagueMembershipId = membership.Id,
                    TownId = town.Id,
                    HeroId = null, // No hero data in historical CSV
                    PlayerColor = playerColor,
                    Position = isWinner ? 1 : 2, // Simple: winner = 1, others = 2
                    IsWinner = isWinner,
                    IsTechnicalLoss = isTechnicalLoss,
                    GoldTrade = participant.StartMoney
                };

                gameParticipants.Add(gameParticipant);
                _dbContext.GameParticipants.Add(gameParticipant);
                importedParticipants++;

                if (isWinner)
                {
                    winnerMembership = membership;
                }
            }

            // Set winner
            if (winnerMembership != null)
            {
                game.WinnerId = winnerMembership.Id;
            }

            // Calculate ratings
            if (gameGroup.IsTechnicalLoss)
            {
                // Technical loss: only penalize the culprit (player with negative result)
                var culpritCsvParticipant = gameGroup.Participants.FirstOrDefault(p => p.Result < 0);
                if (culpritCsvParticipant != null)
                {
                    var culpritParticipant = gameParticipants.First(gp => 
                        _playerResolver.GetMembership(culpritCsvParticipant.Player).Id == gp.LeagueMembershipId);
                    
                    var culpritUpdate = _ratingCalculator.ApplyTechnicalLossPenalty(culpritParticipant);
                    
                    var ratingHistory = _ratingCalculator.CreateRatingHistory(
                        culpritParticipant.LeagueMembershipId,
                        game.Id,
                        culpritUpdate,
                        game.StartTime);
                    
                    _dbContext.RatingHistory.Add(ratingHistory);
                    ratingHistoryCount++;
                }
            }
            else if (winnerMembership != null)
            {
                // Normal game: calculate ratings for all participants
                var ratingUpdates = _ratingCalculator.CalculateGameRatings(
                    gameParticipants.ToList(),
                    winnerMembership.Id);

                foreach (var update in ratingUpdates)
                {
                    var ratingHistory = _ratingCalculator.CreateRatingHistory(
                        update.Key,
                        game.Id,
                        update.Value,
                        game.StartTime);
                    
                    _dbContext.RatingHistory.Add(ratingHistory);
                    ratingHistoryCount++;
                }
            }

            importedGames++;

            if (importedGames % 10 == 0)
            {
                Console.Write($"\rImported {importedGames}/{gameGroups.Count} games...");
            }
        }

        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"\rImported {importedGames}/{gameGroups.Count} games... Done!");
    }

    private void PrintDryRunSummary(
        List<GameGroup> gameGroups, 
        League league, 
        ImportResult result,
        Dictionary<string, Town> towns)
    {
        Console.WriteLine("=== DRY RUN - Import Preview ===\n");
        Console.WriteLine($"CSV File: {result.TotalParticipants} records");
        Console.WriteLine($"League: {league.Name} ({league.Id})");
        Console.WriteLine($"Owner: {league.OwnerId}\n");

        Console.WriteLine($"Players ({result.Players.Count}):");
        foreach (var player in result.Players)
        {
            Console.WriteLine($"  - {player}");
        }

        Console.WriteLine($"\nGames Summary:");
        Console.WriteLine($"  - Total games: {result.TotalGames}");
        Console.WriteLine($"  - Non-technical games: {gameGroups.Count(g => !g.IsTechnicalLoss)}");
        Console.WriteLine($"  - Technical loss games: {gameGroups.Count(g => g.IsTechnicalLoss)}");
        Console.WriteLine($"  - Date range: {gameGroups.Min(g => g.GameDate):yyyy-MM-dd} to {gameGroups.Max(g => g.GameDate):yyyy-MM-dd}");

        var sundays = gameGroups.Select(g => g.GameDate.Date).Distinct().Count();
        Console.WriteLine($"  - Sundays: {sundays}\n");

        Console.WriteLine("Sample Games (first 3):");
        foreach (var game in gameGroups.Take(3))
        {
            Console.WriteLine($"\n  Game ({game.GameDate:yyyy-MM-dd HH:mm}) - Map: {game.MapName}");
            var winner = game.Participants.FirstOrDefault(p => p.Result == 1);
            if (winner != null)
            {
                Console.WriteLine($"    Winner: {winner.Player}");
            }
            Console.WriteLine("    Participants:");
            foreach (var p in game.Participants)
            {
                var status = p.Result == 1 ? "WIN" : p.IsTechnicalLoss == 1 ? "TECH LOSS" : "LOSS";
                Console.WriteLine($"      • {p.Player} ({p.City}, {p.Color}) - {status} - Gold: {p.StartMoney}");
            }
        }

        Console.WriteLine("\n\nValidations:");
        var missingTowns = gameGroups
            .SelectMany(g => g.Participants.Select(p => p.City))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(city => !towns.ContainsKey(city))
            .ToList();

        if (missingTowns.Any())
        {
            Console.WriteLine($"  ✗ Missing towns in database: {string.Join(", ", missingTowns)}");
        }
        else
        {
            Console.WriteLine("  ✓ All towns found in database");
        }

        Console.WriteLine("  ✓ All colors valid");
        Console.WriteLine($"  ⚠ Warning: {result.TotalParticipants} participants will have null Hero");

        Console.WriteLine("\n=== This is a DRY RUN - no changes will be made ===");
    }

    private async Task UpdateFinalRatingsAsync(Guid leagueId)
    {
        // Get all memberships for this league
        var memberships = await _dbContext.LeagueMemberships
            .Where(lm => lm.LeagueId == leagueId)
            .ToListAsync();

        int updatedCount = 0;

        foreach (var membership in memberships)
        {
            // Find the most recent rating history entry for this member
            var latestRatingHistory = await _dbContext.RatingHistory
                .Where(rh => rh.LeagueMembershipId == membership.Id)
                .OrderByDescending(rh => rh.CalculatedAt)
                .FirstOrDefaultAsync();

            if (latestRatingHistory != null)
            {
                // Update the league membership with the final ratings
                membership.Glicko2Rating = latestRatingHistory.NewRating;
                membership.Glicko2RatingDeviation = latestRatingHistory.NewRatingDeviation;
                membership.Glicko2Volatility = latestRatingHistory.NewVolatility;
                membership.LastRatingUpdate = latestRatingHistory.CalculatedAt;

                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Updated current ratings for {updatedCount}/{memberships.Count} players");
        }
        else
        {
            Console.WriteLine("No rating updates needed (no RatingHistory entries found)");
        }
    }

    private PlayerColor ParsePlayerColor(string color)
    {
        return color.ToLower() switch
        {
            "red" => PlayerColor.Red,
            "blue" => PlayerColor.Blue,
            "tan" => PlayerColor.Tan,
            "green" => PlayerColor.Green,
            "orange" => PlayerColor.Orange,
            "purple" => PlayerColor.Purple,
            "teal" => PlayerColor.Teal,
            "pink" => PlayerColor.Pink,
            _ => throw new InvalidOperationException($"Invalid color: {color}")
        };
    }
}

public class ImportResult
{
    public int TotalGames { get; set; }
    public int TotalParticipants { get; set; }
    public string LeagueName { get; set; } = string.Empty;
    public List<string> Players { get; set; } = new();
}
