namespace ScoreBurrow.Rating.Core;

using ScoreBurrow.Rating.Models;

/// <summary>
/// Core Glicko-2 rating system implementation.
/// Based on the paper by Mark Glickman: http://www.glicko.net/glicko/glicko2.pdf
/// </summary>
public class Glicko2Calculator
{
    private readonly double _tau;

    public Glicko2Calculator(double tau = Glicko2Constants.Tau)
    {
        _tau = tau;
    }

    /// <summary>
    /// Calculates new rating based on a collection of match results
    /// </summary>
    public RatingUpdate CalculateNewRating(RatingSnapshot currentRating, List<GameMatchup> matchups)
    {
        if (matchups == null || matchups.Count == 0)
        {
            // No games played - only update rating deviation due to time
            return UpdateInactiveRating(currentRating);
        }

        // Convert to Glicko-2 scale
        var (mu, phi, sigma) = currentRating.ToGlicko2Scale();

        // Step 1: Calculate variance (v)
        double v = CalculateVariance(mu, phi, matchups);

        // Step 2: Calculate delta (improvement)
        double delta = CalculateDelta(mu, phi, matchups, v);

        // Step 3: Update volatility
        double newSigma = UpdateVolatility(phi, sigma, delta, v);

        // Step 4: Update rating deviation
        double phiStar = Math.Sqrt(phi * phi + newSigma * newSigma);

        // Step 5: Update rating and rating deviation
        double newPhi = 1.0 / Math.Sqrt(1.0 / (phiStar * phiStar) + 1.0 / v);
        double newMu = mu + newPhi * newPhi * SumDeltaScores(mu, phi, matchups);

        // Convert back to Glicko scale
        var newRating = RatingSnapshot.FromGlicko2Scale(newMu, newPhi, newSigma);

        // Clamp rating deviation to reasonable bounds
        newRating = ClampRatingDeviation(newRating);

        return new RatingUpdate(currentRating, newRating);
    }

    /// <summary>
    /// Updates rating for inactive player (no games played in rating period)
    /// Only rating deviation increases due to uncertainty
    /// </summary>
    private RatingUpdate UpdateInactiveRating(RatingSnapshot currentRating)
    {
        var (mu, phi, sigma) = currentRating.ToGlicko2Scale();
        
        // Increase phi due to inactivity
        double newPhi = Math.Sqrt(phi * phi + sigma * sigma);
        
        var newRating = RatingSnapshot.FromGlicko2Scale(mu, newPhi, sigma);
        newRating = ClampRatingDeviation(newRating);
        
        return new RatingUpdate(currentRating, newRating);
    }

    /// <summary>
    /// Calculates the g function - reduces impact of opponent's rating based on their RD
    /// </summary>
    private double G(double opponentPhi)
    {
        return 1.0 / Math.Sqrt(1.0 + 3.0 * opponentPhi * opponentPhi / (Math.PI * Math.PI));
    }

    /// <summary>
    /// Calculates expected score against an opponent
    /// </summary>
    private double E(double mu, double opponentMu, double opponentPhi)
    {
        return 1.0 / (1.0 + Math.Exp(-G(opponentPhi) * (mu - opponentMu)));
    }

    /// <summary>
    /// Calculates variance of player's rating based on game outcomes
    /// </summary>
    private double CalculateVariance(double mu, double phi, List<GameMatchup> matchups)
    {
        double sum = 0.0;

        foreach (var matchup in matchups)
        {
            var (opponentMu, opponentPhi, _) = matchup.OpponentRating.ToGlicko2Scale();
            double gPhi = G(opponentPhi);
            double expectedScore = E(mu, opponentMu, opponentPhi);
            
            sum += gPhi * gPhi * expectedScore * (1.0 - expectedScore);
        }

        return 1.0 / sum;
    }

    /// <summary>
    /// Calculates delta - the estimated improvement in rating
    /// </summary>
    private double CalculateDelta(double mu, double phi, List<GameMatchup> matchups, double v)
    {
        return v * SumDeltaScores(mu, phi, matchups);
    }

    /// <summary>
    /// Sums up the performance differences weighted by g function
    /// </summary>
    private double SumDeltaScores(double mu, double phi, List<GameMatchup> matchups)
    {
        double sum = 0.0;

        foreach (var matchup in matchups)
        {
            var (opponentMu, opponentPhi, _) = matchup.OpponentRating.ToGlicko2Scale();
            double gPhi = G(opponentPhi);
            double expectedScore = E(mu, opponentMu, opponentPhi);
            
            sum += gPhi * (matchup.Score - expectedScore);
        }

        return sum;
    }

    /// <summary>
    /// Updates volatility using iterative algorithm (Illinois method)
    /// This is the most complex part of Glicko-2
    /// </summary>
    private double UpdateVolatility(double phi, double sigma, double delta, double v)
    {
        double a = Math.Log(sigma * sigma);
        double tau2 = _tau * _tau;
        double phi2 = phi * phi;
        double delta2 = delta * delta;

        // Function f(x) from the paper
        double F(double x)
        {
            double ex = Math.Exp(x);
            double phi2ex = phi2 + v + ex;
            
            double term1 = ex * (delta2 - phi2 - v - ex) / (2.0 * phi2ex * phi2ex);
            double term2 = (x - a) / tau2;
            
            return term1 - term2;
        }

        // Find bounds
        double A = a;
        double B;
        
        if (delta2 > phi2 + v)
        {
            B = Math.Log(delta2 - phi2 - v);
        }
        else
        {
            int k = 1;
            while (F(a - k * _tau) < 0)
            {
                k++;
            }
            B = a - k * _tau;
        }

        // Illinois algorithm
        double fA = F(A);
        double fB = F(B);

        while (Math.Abs(B - A) > Glicko2Constants.ConvergenceTolerance)
        {
            double C = A + (A - B) * fA / (fB - fA);
            double fC = F(C);

            if (fC * fB < 0)
            {
                A = B;
                fA = fB;
            }
            else
            {
                fA = fA / 2.0;
            }

            B = C;
            fB = fC;
        }

        return Math.Exp(A / 2.0);
    }

    /// <summary>
    /// Clamps rating deviation to reasonable bounds
    /// </summary>
    private RatingSnapshot ClampRatingDeviation(RatingSnapshot rating)
    {
        double rd = rating.RatingDeviation;
        
        if (rd < Glicko2Constants.MinRatingDeviation)
        {
            rd = Glicko2Constants.MinRatingDeviation;
        }
        else if (rd > Glicko2Constants.MaxRatingDeviation)
        {
            rd = Glicko2Constants.MaxRatingDeviation;
        }

        if (Math.Abs(rd - rating.RatingDeviation) < 0.001)
        {
            return rating;
        }

        return new RatingSnapshot(rating.Rating, rd, rating.Volatility);
    }
}
