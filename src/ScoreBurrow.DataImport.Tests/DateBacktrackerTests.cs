using FluentAssertions;
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
}
