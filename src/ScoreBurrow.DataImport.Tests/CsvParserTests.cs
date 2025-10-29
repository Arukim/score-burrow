using FluentAssertions;
using ScoreBurrow.DataImport.Models;
using ScoreBurrow.DataImport.Services;

namespace ScoreBurrow.DataImport.Tests;

public class CsvParserTests
{
    [Fact]
    public void GroupGamesByRedColor_ThreeGames_ReturnsThreeGroups()
    {
        // Arrange
        var parser = new CsvParser();
        var records = new List<CsvGameRecord>
        {
            new() { Player = "Player1", City = "Castle", Color = "Red", MapName = "XL" },
            new() { Player = "Player2", City = "Tower", Color = "Blue", MapName = "XL" },
            new() { Player = "Player3", City = "Fortress", Color = "Teal", MapName = "XL" },
            new() { Player = "Player1", City = "Dungeon", Color = "Red", MapName = "L" },
            new() { Player = "Player2", City = "Necropolis", Color = "Blue", MapName = "L" },
            new() { Player = "Player3", City = "Castle", Color = "Red", MapName = "M" },
        };

        // Act
        var result = parser.GroupGamesByRedColor(records);

        // Assert
        result.Should().HaveCount(3);
        result[0].Participants.Should().HaveCount(3);
        result[1].Participants.Should().HaveCount(2);
        result[2].Participants.Should().HaveCount(1);
    }

    [Fact]
    public void GroupGamesByRedColor_SingleGame_ReturnsSingleGroup()
    {
        // Arrange
        var parser = new CsvParser();
        var records = new List<CsvGameRecord>
        {
            new() { Player = "Player1", City = "Castle", Color = "Red", MapName = "XL" },
            new() { Player = "Player2", City = "Tower", Color = "Blue", MapName = "XL" },
        };

        // Act
        var result = parser.GroupGamesByRedColor(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].Participants.Should().HaveCount(2);
        result[0].MapName.Should().Be("XL");
    }

    [Fact]
    public void GroupGamesByRedColor_WithTechnicalLosses_GroupsCorrectly()
    {
        // Arrange
        var parser = new CsvParser();
        var records = new List<CsvGameRecord>
        {
            new() { Player = "Player1", City = "Castle", Color = "Red", Result = 1, IsTechnicalLoss = 0, MapName = "XL" },
            new() { Player = "Player2", City = "Tower", Color = "Blue", Result = 0, IsTechnicalLoss = 0, MapName = "XL" },
            new() { Player = "Player1", City = "Dungeon", Color = "Red", Result = -0.25m, IsTechnicalLoss = 1, MapName = "L" },
            new() { Player = "Player2", City = "Necropolis", Color = "Blue", Result = 0, IsTechnicalLoss = 1, MapName = "L" },
        };

        // Act
        var result = parser.GroupGamesByRedColor(records);

        // Assert
        result.Should().HaveCount(2);
        result[0].IsTechnicalLoss.Should().BeFalse();
        result[1].IsTechnicalLoss.Should().BeTrue();
    }

    [Fact]
    public void ValidateGameGroups_AllValid_DoesNotThrow()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>
                {
                    new() { Player = "Player1", City = "Castle", Color = "Red" },
                    new() { Player = "Player2", City = "Tower", Color = "Blue" },
                }
            }
        };

        // Act
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateGameGroups_InvalidColor_ThrowsException()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>
                {
                    new() { Player = "Player1", City = "Castle", Color = "Red", StartMoney = 1000 },
                    new() { Player = "Player2", City = "Tower", Color = "InvalidColor", StartMoney = -1000 },
                }
            }
        };

        // Act
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid color*");
    }

    [Fact]
    public void ValidateGameGroups_NoParticipants_ThrowsException()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>()
            }
        };

        // Act
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*has no participants*");
    }

    [Fact]
    public void ValidateGameGroups_DuplicateColors_ThrowsException()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>
                {
                    new() { Player = "Player1", City = "Castle", Color = "Red", StartMoney = 1000 },
                    new() { Player = "Player2", City = "Tower", Color = "Blue", StartMoney = -2000 },
                    new() { Player = "Player3", City = "Fortress", Color = "Blue", StartMoney = 1000 },
                }
            }
        };

        // Act
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*has duplicate colors*");
    }

    [Fact]
    public void ValidateGameGroups_NoRedPlayer_ThrowsException()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>
                {
                    new() { Player = "Player1", City = "Castle", Color = "Blue", StartMoney = 1000 },
                    new() { Player = "Player2", City = "Tower", Color = "Teal", StartMoney = -1000 },
                }
            }
        };

        // Act
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have exactly one Red player*");
    }

    [Fact]
    public void ValidateGameGroups_NoBluePlayer_ThrowsException()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>
                {
                    new() { Player = "Player1", City = "Castle", Color = "Red", StartMoney = 1000 },
                    new() { Player = "Player2", City = "Tower", Color = "Teal", StartMoney = -1000 },
                }
            }
        };

        // Act
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have exactly one Blue player*");
    }

    [Fact]
    public void ValidateGameGroups_UnbalancedGoldTrade_ShowsWarning()
    {
        // Arrange
        var parser = new CsvParser();
        var gameGroups = new List<GameGroup>
        {
            new()
            {
                MapName = "XL",
                Participants = new List<CsvGameRecord>
                {
                    new() { Player = "Player1", City = "Castle", Color = "Red", StartMoney = 1000, RowNumber = 1 },
                    new() { Player = "Player2", City = "Tower", Color = "Blue", StartMoney = -500, RowNumber = 2 },
                }
            }
        };

        // Act - Should not throw, just show warning
        var act = () => parser.ValidateGameGroups(gameGroups);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GroupGamesByRedColor_RealWorldSample_Creates4Games()
    {
        // Arrange - Real-world sample from user
        var parser = new CsvParser();
        var records = new List<CsvGameRecord>
        {
            // Game 1 (rows 2-4): Red, Blue, Teal
            new() { Player = "Arukim", City = "Dungeon", StartMoney = -5000, Color = "Red", Result = 1, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 2 },
            new() { Player = "eazy-f", City = "Tower", StartMoney = 6000, Color = "Blue", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 3 },
            new() { Player = "Lord of Titty", City = "Fortress", StartMoney = -1000, Color = "Teal", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 4 },
            // Game 2 (rows 5-7): Red, Blue, Teal
            new() { Player = "eazy-f", City = "Castle", StartMoney = -5000, Color = "Red", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 5 },
            new() { Player = "Arukim", City = "Necropolis", StartMoney = 5500, Color = "Blue", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 6 },
            new() { Player = "Lord of Titty", City = "Conflux", StartMoney = -500, Color = "Teal", Result = 1, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 7 },
            // Game 3 (rows 8-10): Blue, Red, Teal (Blue triggers new game)
            new() { Player = "Arukim", City = "Castle", StartMoney = 6500, Color = "Blue", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 8 },
            new() { Player = "Lord of Titty", City = "Necropolis", StartMoney = -5750, Color = "Red", Result = 1, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 9 },
            new() { Player = "eazy-f", City = "Fortress", StartMoney = -750, Color = "Teal", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 10 },
            // Game 4 (rows 11-13): Teal, Red, Blue (Teal triggers new game)
            new() { Player = "Arukim", City = "Inferno", StartMoney = -5250, Color = "Teal", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 11 },
            new() { Player = "Lord of Titty", City = "Rampart", StartMoney = 8750, Color = "Red", Result = 0, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 12 },
            new() { Player = "eazy-f", City = "Cove", StartMoney = -3500, Color = "Blue", Result = 1, IsTechnicalLoss = 0, MapName = "XL", RowNumber = 13 },
        };

        // Act
        var result = parser.GroupGamesByRedColor(records);

        // Assert
        result.Should().HaveCount(4);
        result[0].Participants.Should().HaveCount(3);
        result[1].Participants.Should().HaveCount(3);
        result[2].Participants.Should().HaveCount(3);
        result[3].Participants.Should().HaveCount(3);

        // Verify no validation errors
        var act = () => parser.ValidateGameGroups(result);
        act.Should().NotThrow();
    }
}
