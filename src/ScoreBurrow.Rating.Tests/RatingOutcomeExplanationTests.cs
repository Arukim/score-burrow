using FluentAssertions;
using ScoreBurrow.Rating.Models;

namespace ScoreBurrow.Rating.Tests;

public class RatingOutcomeExplanationTests
{
    [Fact]
    public void LowerRatedPlayerWithHigherDeviation_ExplainsTheLargerLoss()
    {
        var notes = RatingOutcomeExplanation.Explain(new[]
        {
            Player("Lord of Titty", 1657.6, 60, -15.6),
            Player("Arukim", 1582.8, 66, -15.8),
            Player("Sfay", 1379.5, 60, 43.5, isWinner: true),
            Player("eazy-f", 1380.2, 60, -12.2)
        });

        notes.Should().Contain(RatingOutcomeExplanation.DeviationMeaning);
        notes.Should().Contain(note =>
            note.Contains("Arukim's change was -15.8", StringComparison.Ordinal) &&
            note.Contains("Lord of Titty's was -15.6", StringComparison.Ordinal) &&
            note.Contains("deviation was 66.0", StringComparison.Ordinal) &&
            note.Contains("60.0", StringComparison.Ordinal));
        notes.Should().NotContain(note => note.Contains("largest loss", StringComparison.Ordinal));
    }

    [Fact]
    public void HigherRatedLoser_ExplainsTheMoreExpectedWin()
    {
        var notes = RatingOutcomeExplanation.Explain(new[]
        {
            Player("Lord of Titty", 1657.6, 60, -16.6),
            Player("Arukim", 1582.8, 60, -15.1),
            Player("Sfay", 1379.5, 60, 41.5, isWinner: true),
            Player("eazy-f", 1380.2, 60, -9.8)
        });

        notes.Should().Contain(note =>
            note.Contains("Lord of Titty's change was -16.6", StringComparison.Ordinal) &&
            note.Contains("largest loss", StringComparison.Ordinal) &&
            note.Contains("more expected", StringComparison.Ordinal));
    }

    [Fact]
    public void EqualDeviation_DoesNotBlameDeviationForATinyGap()
    {
        var notes = RatingOutcomeExplanation.Explain(new[]
        {
            Player("Lord of Titty", 1657.6, 60, -15.6),
            Player("Arukim", 1582.8, 60.2, -15.8),
            Player("Sfay", 1379.5, 60, 43.6, isWinner: true)
        });

        notes.Should().NotContain(note => note.Contains("less certain", StringComparison.Ordinal));
    }

    [Fact]
    public void TechnicalLoss_ExplainsTheSplitPenalty()
    {
        var notes = RatingOutcomeExplanation.Explain(new[]
        {
            Player("Sfay", 1420, 55, -18.0, isTechnicalLoss: true),
            Player("Arukim", 1560, 50, 9.0),
            Player("eazy-f", 1380, 70, 9.0)
        });

        notes.Should().Contain(note =>
            note.Contains("Sfay's change was -18.0", StringComparison.Ordinal) &&
            note.Contains("technical loss", StringComparison.Ordinal) &&
            note.Contains("1420.0 ± 55.0", StringComparison.Ordinal));
        notes.Should().Contain(note =>
            note.Contains("split equally", StringComparison.Ordinal) &&
            note.Contains("deviation stays the same", StringComparison.Ordinal));
    }

    [Fact]
    public void HigherRatedLargerLoss_StaysWithExpectedWin_WhenDeviationIsHigherOnTheOtherLoser()
    {
        var notes = RatingOutcomeExplanation.Explain(new[]
        {
            Player("Lord of Titty", 1657.6, 60, -20.0),
            Player("Arukim", 1582.8, 90, -12.0),
            Player("Sfay", 1379.5, 60, 32.0, isWinner: true)
        });

        notes.Should().Contain(note => note.Contains("largest loss", StringComparison.Ordinal));
        notes.Should().NotContain(note => note.Contains("less certain", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyGame_HasNoNotes()
    {
        RatingOutcomeExplanation.Explain(Array.Empty<RatingOutcomePlayer>()).Should().BeEmpty();
    }

    private static RatingOutcomePlayer Player(
        string name,
        double rating,
        double deviation,
        double change,
        bool isWinner = false,
        bool isTechnicalLoss = false)
    {
        return new RatingOutcomePlayer(name, rating, deviation, deviation, change, isWinner, isTechnicalLoss);
    }
}
