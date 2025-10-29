using ScoreBurrow.DataImport.Models;

namespace ScoreBurrow.DataImport.Services;

public class DateBacktracker
{
    private readonly Random _random = new();

    public DateTime GetPreviousSunday(DateTime date)
    {
        var daysSinceLastSunday = ((int)date.DayOfWeek + 7) % 7;
        if (daysSinceLastSunday == 0 && date.TimeOfDay == TimeSpan.Zero)
        {
            // If it's exactly Sunday at midnight, return the same date
            return date;
        }
        return date.Date.AddDays(-daysSinceLastSunday);
    }

    public void AssignGameDates(List<GameGroup> gameGroups)
    {
        var startDate = GetPreviousSunday(DateTime.Now);
        
        // Filter non-technical loss games for distribution
        var nonTechGames = gameGroups.Where(g => !g.IsTechnicalLoss).ToList();
        
        // Reverse so latest games in CSV get most recent dates
        nonTechGames.Reverse();
        
        // Distribute games across Sundays
        var sundayGames = DistributeGamesAcrossSundays(nonTechGames, startDate);
        
        // Assign dates to technical loss games based on their non-tech counterparts
        AssignTechnicalLossDates(gameGroups, sundayGames);
    }

    private Dictionary<DateTime, List<GameGroup>> DistributeGamesAcrossSundays(
        List<GameGroup> nonTechGames, 
        DateTime startDate)
    {
        var sundayGames = new Dictionary<DateTime, List<GameGroup>>();
        var currentSunday = startDate;
        var remainingGames = new List<GameGroup>(nonTechGames);

        while (remainingGames.Any())
        {
            // Randomly select 1-2 games for this Sunday (max 2 games per playday)
            var gamesThisSunday = _random.Next(1, Math.Min(3, remainingGames.Count + 1));
            var sundayGameList = new List<GameGroup>();
            var townsUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < gamesThisSunday && remainingGames.Any(); i++)
            {
                // Find a game that doesn't use a town already played this Sunday
                var availableGame = remainingGames.FirstOrDefault(g =>
                    !g.Participants.Any(p => townsUsed.Contains(p.City)));

                if (availableGame == null)
                {
                    // No valid game found (all would use duplicate towns), move to next Sunday
                    break;
                }

                // Add this game to the Sunday
                sundayGameList.Add(availableGame);
                remainingGames.Remove(availableGame);

                // Track towns used
                foreach (var participant in availableGame.Participants)
                {
                    townsUsed.Add(participant.City);
                }
            }

            // Assign times (1:00 PM, 2:00 PM)
            for (int i = 0; i < sundayGameList.Count; i++)
            {
                sundayGameList[i].GameDate = currentSunday.AddHours(13 + i);
            }

            sundayGames[currentSunday] = sundayGameList;

            // Move to previous Sunday
            currentSunday = currentSunday.AddDays(-7);
        }

        return sundayGames;
    }

    private void AssignTechnicalLossDates(
        List<GameGroup> allGames, 
        Dictionary<DateTime, List<GameGroup>> sundayGames)
    {
        var techLossGames = allGames.Where(g => g.IsTechnicalLoss).ToList();
        
        // For each technical loss game, find its closest non-tech game and use same date
        foreach (var techGame in techLossGames)
        {
            var gameIndex = allGames.IndexOf(techGame);
            
            // Look backward for the closest non-tech game
            GameGroup? closestNonTech = null;
            for (int i = gameIndex - 1; i >= 0; i--)
            {
                if (!allGames[i].IsTechnicalLoss && allGames[i].GameDate != DateTime.MinValue)
                {
                    closestNonTech = allGames[i];
                    break;
                }
            }

            // If not found backward, look forward
            if (closestNonTech == null)
            {
                for (int i = gameIndex + 1; i < allGames.Count; i++)
                {
                    if (!allGames[i].IsTechnicalLoss && allGames[i].GameDate != DateTime.MinValue)
                    {
                        closestNonTech = allGames[i];
                        break;
                    }
                }
            }

            // Assign same date as closest non-tech game (or default to last Sunday if none found)
            if (closestNonTech != null)
            {
                techGame.GameDate = closestNonTech.GameDate;
            }
            else
            {
                // Fallback: assign to the earliest Sunday
                var earliestSunday = sundayGames.Keys.OrderBy(k => k).FirstOrDefault();
                techGame.GameDate = earliestSunday != default ? earliestSunday.AddHours(13) : GetPreviousSunday(DateTime.Now);
            }
        }
    }
}
