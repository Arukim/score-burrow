using FluentAssertions;
using ScoreBurrow.DataImport.Models;
using ScoreBurrow.DataImport.Services;

namespace ScoreBurrow.DataImport.Tests;

public class DateBacktrackerTests
{
    [Fact]
    public void GetPreviousSunday_FromMonday_ReturnsCorrectDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var monday = new DateTime(2025, 10, 27); // Monday

        // Act
        var result = backtracker.GetPreviousSunday(monday);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(new DateTime(2025, 10, 26)); // Previous Sunday
    }

    [Fact]
    public void GetPreviousSunday_FromSunday_ReturnsSameDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var sunday = new DateTime(2025, 10, 26); // Sunday

        // Act
        var result = backtracker.GetPreviousSunday(sunday);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(sunday);
    }

    [Fact]
    public void GetPreviousSunday_FromSaturday_ReturnsCorrectDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var saturday = new DateTime(2025, 11, 1); // Saturday

        // Act
        var result = backtracker.GetPreviousSunday(saturday);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(new DateTime(2025, 10, 26)); // Previous Sunday
    }

    [Fact]
    public void GetPreviousSunday_FromFriday_ReturnsCorrectDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var friday = new DateTime(2025, 10, 31); // Friday

        // Act
        var result = backtracker.GetPreviousSunday(friday);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(new DateTime(2025, 10, 26)); // Previous Sunday
    }

    [Fact]
    public void GetPreviousSunday_AcrossMonthBoundary_ReturnsCorrectDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var wednesday = new DateTime(2025, 11, 5); // Wednesday

        // Act
        var result = backtracker.GetPreviousSunday(wednesday);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(new DateTime(2025, 11, 2)); // Previous Sunday
    }

    [Fact]
    public void GetPreviousSunday_AcrossYearBoundary_ReturnsCorrectDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var thursday = new DateTime(2026, 1, 1); // Thursday

        // Act
        var result = backtracker.GetPreviousSunday(thursday);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(new DateTime(2025, 12, 28)); // Previous Sunday in 2025
    }

    [Fact]
    public void AssignGameDates_MaintainsCsvOrder_LatestGameGetsRecentDate()
    {
        // Arrange
        var backtracker = new DateBacktracker();
        var gameGroups = new List<GameGroup>
        {
            // Game 1 (oldest - should get oldest Sunday)
            new GameGroup
            {
                MapName = "Map1",
                Participants = new List<CsvGameRecord>
                {
                    new CsvGameRecord { Player = "Player1", City = "Castle", Color = "Red", Result = 1 },
                    new CsvGameRecord { Player = "Player2", City = "Tower", Color = "Blue", Result = 0 }
                }
            },
            // Game 2 - uses Castle again to force different Sunday
            new GameGroup
            {
                MapName = "Map2",
                Participants = new List<CsvGameRecord>
                {
                    new CsvGameRecord { Player = "Player3", City = "Castle", Color = "Red", Result = 1 },
                    new CsvGameRecord { Player = "Player4", City = "Inferno", Color = "Blue", Result = 0 }
                }
            },
            // Game 3 - uses Castle again to force different Sunday
            new GameGroup
            {
                MapName = "Map3",
                Participants = new List<CsvGameRecord>
                {
                    new CsvGameRecord { Player = "Player5", City = "Castle", Color = "Red", Result = 1 },
                    new CsvGameRecord { Player = "Player6", City = "Rampart", Color = "Blue", Result = 0 }
                }
            },
            // Game 4 (newest - should get most recent Sunday)
            new GameGroup
            {
                MapName = "Map4",
                Participants = new List<CsvGameRecord>
                {
                    new CsvGameRecord { Player = "Player7", City = "Castle", Color = "Red", Result = 1 },
                    new CsvGameRecord { Player = "Player8", City = "Fortress", Color = "Blue", Result = 0 }
                }
            }
        };

        // Act
        backtracker.AssignGameDates(gameGroups);

        // Assert
        var firstGame = gameGroups[0]; // Oldest game in CSV
        var lastGame = gameGroups[3];  // Latest game in CSV

        // The latest game in CSV should have a more recent date than the first
        lastGame.GameDate.Should().BeAfter(firstGame.GameDate, 
            "latest game in CSV should get a more recent date than the oldest");
        
        // Games should be in chronological order matching CSV order
        for (int i = 1; i < gameGroups.Count; i++)
        {
            gameGroups[i].GameDate.Should().BeOnOrAfter(gameGroups[i-1].GameDate,
                $"game {i} should have a date on or after game {i-1} to maintain CSV chronological order");
        }
        
        // All games should have dates assigned
        gameGroups.Should().AllSatisfy(g => 
            g.GameDate.Should().NotBe(DateTime.MinValue, "all games should have dates assigned"));

        // All dates should be on Sundays
        gameGroups.Should().AllSatisfy(g => 
            g.GameDate.DayOfWeek.Should().Be(DayOfWeek.Sunday, "all games should be on Sundays"));
    }
}
