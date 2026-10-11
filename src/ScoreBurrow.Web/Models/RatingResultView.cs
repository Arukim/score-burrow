namespace ScoreBurrow.Web.Models;

public class RatingResultView
{
    public double PreviousRating { get; set; }
    public double PreviousDeviation { get; set; }
    public double NewRating { get; set; }
    public double NewDeviation { get; set; }
    public double Change => NewRating - PreviousRating;
}
