using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ScoreBurrow.DataImport.Models;

namespace ScoreBurrow.DataImport.Services;

public class CsvParser
{
    public List<CsvGameRecord> ParseCsv(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        var records = new List<CsvGameRecord>();
        var rowNumber = 2; // Start at 2 (1 is header)
        
        while (csv.Read())
        {
            var record = csv.GetRecord<CsvGameRecord>();
            if (record != null)
            {
                record.RowNumber = rowNumber;
                records.Add(record);
            }
            rowNumber++;
        }
        
        if (records.Count == 0)
        {
            throw new InvalidOperationException("CSV file is empty or has no valid records");
        }

        return records;
    }

    public List<GameGroup> GroupGamesByRedColor(List<CsvGameRecord> records)
    {
        var gameGroups = new List<GameGroup>();
        var currentGame = new List<CsvGameRecord>();

        foreach (var record in records)
        {
            // Check if this color already exists in current game (duplicate color = new game)
            var colorExists = currentGame.Any(p => p.Color.Equals(record.Color, StringComparison.OrdinalIgnoreCase));
            
            // Check if this player already exists in current game (duplicate player = new game)
            var playerExists = currentGame.Any(p => p.Player.Equals(record.Player, StringComparison.OrdinalIgnoreCase));
            
            // Duplicate color OR duplicate player means start of new game
            if (colorExists || playerExists)
            {
                // Save previous game if it exists
                if (currentGame.Count > 0)
                {
                    gameGroups.Add(new GameGroup
                    {
                        MapName = currentGame[0].MapName,
                        Participants = new List<CsvGameRecord>(currentGame)
                    });
                    currentGame.Clear();
                }
            }

            currentGame.Add(record);
        }

        // Add the last game
        if (currentGame.Count > 0)
        {
            gameGroups.Add(new GameGroup
            {
                MapName = currentGame[0].MapName,
                Participants = new List<CsvGameRecord>(currentGame)
            });
        }

        return gameGroups;
    }

    public void ValidateGameGroups(List<GameGroup> gameGroups)
    {
        var validColors = new[] { "Red", "Blue", "Teal", "Green", "Orange", "Purple", "Pink", "Tan" };

        foreach (var game in gameGroups)
        {
            var rows = string.Join(", ", game.Participants.Select(p => p.RowNumber));
            
            // Check if game has participants
            if (game.Participants.Count == 0)
            {
                throw new InvalidOperationException($"Game with map {game.MapName} has no participants");
            }

            // Validate colors
            foreach (var participant in game.Participants)
            {
                if (!validColors.Contains(participant.Color, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid color '{participant.Color}' at row {participant.RowNumber} (Player: {participant.Player}, Map: {game.MapName})");
                }
            }

            // Check for duplicate colors in same game
            var colorCounts = game.Participants
                .GroupBy(p => p.Color, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            if (colorCounts.Any())
            {
                var duplicates = string.Join(", ", colorCounts.Select(g => g.Key));
                throw new InvalidOperationException($"Game on map {game.MapName} (rows {rows}) has duplicate colors: {duplicates}");
            }

            // Check for exactly one Red player
            var redCount = game.Participants.Count(p => p.Color.Equals("Red", StringComparison.OrdinalIgnoreCase));
            if (redCount != 1)
            {
                throw new InvalidOperationException($"Game on map {game.MapName} (rows {rows}) must have exactly one Red player, found {redCount}");
            }

            // Check for exactly one Blue player
            var blueCount = game.Participants.Count(p => p.Color.Equals("Blue", StringComparison.OrdinalIgnoreCase));
            if (blueCount != 1)
            {
                throw new InvalidOperationException($"Game on map {game.MapName} (rows {rows}) must have exactly one Blue player, found {blueCount}");
            }

            // Check that sum of Start money equals 0 (warning only, not an error)
            var totalMoney = game.Participants.Sum(p => p.StartMoney);
            if (totalMoney != 0)
            {
                var participantDetails = string.Join(", ", game.Participants.Select(p => $"Row {p.RowNumber}: {p.Player}({p.StartMoney})"));
                Console.WriteLine($"Warning: Game on map {game.MapName} (rows {rows}) has unbalanced gold trade (sum: {totalMoney}). Participants: {participantDetails}");
            }
        }
    }
}
