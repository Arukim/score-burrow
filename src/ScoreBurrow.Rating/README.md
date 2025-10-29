# ScoreBurrow.Rating

A .NET library implementing the Glicko-2 rating system for Heroes of Might and Magic 3 multiplayer games.

## Overview

This project provides a complete Glicko-2 rating calculation system adapted for multi-player games where there is one winner and N-1 losers. It also includes a special mechanism for handling technical losses.

## Features

- **Glicko-2 Rating System**: Industry-standard rating algorithm with volatility tracking
- **Multi-Player Adaptation**: Winner plays N-1 virtual matches against each loser
- **Technical Loss Penalty**: Players who cause technical losses play against themselves and lose
- **Rating History**: Complete audit trail of all rating changes
- **Immutable Models**: Thread-safe rating snapshots and updates

## Architecture

```
ScoreBurrow.Rating/
├── Core/
│   ├── Glicko2Calculator.cs      # Core Glicko-2 algorithm implementation
│   └── Glicko2Constants.cs       # Rating system constants
├── Models/
│   ├── MatchOutcome.cs           # Enum for match outcome (Win/Draw/Loss)
│   ├── RatingSnapshot.cs         # Immutable rating state at a point in time
│   ├── GameMatchup.cs            # Single match between two players
│   └── RatingUpdate.cs           # Before/after rating change
└── Services/
    ├── IRatingService.cs         # Rating service interface
    └── RatingService.cs          # Rating calculation service
```

## Rating System Details

### Glicko-2 Parameters

- **Default Rating**: 1500
- **Default Rating Deviation (RD)**: 350
- **Default Volatility**: 0.06
- **Tau (τ)**: 0.5 (system constant)
- **RD Range**: 30-500

### Multi-Player Game Logic

For a normal N-player game with 1 winner:

1. **Winner's Perspective**: Plays N-1 matches, wins all of them
2. **Loser's Perspective**: Plays 1 match against the winner, loses it

This approach ensures:
- Winners gain more rating for beating multiple opponents
- Each loser's rating change reflects losing to the winner only
- The system remains mathematically sound

### Technical Loss Handling

When a player causes a technical loss:

1. Player's rating is snapshot
2. Player "plays against themselves" with that rating
3. Player loses the match
4. Rating penalty is applied
5. Other players' ratings are **not affected**

This creates a natural scaling penalty:
- Higher-rated players lose more rating points
- Lower-rated players lose fewer rating points
- Penalty is consistent with the rating system

## Usage

### Basic Rating Calculation

```csharp
var ratingService = new RatingService();

// Build participant ratings
var participants = new Dictionary<Guid, RatingSnapshot>
{
    { player1Id, new RatingSnapshot(1500, 350, 0.06) },
    { player2Id, new RatingSnapshot(1600, 200, 0.05) },
    { player3Id, new RatingSnapshot(1400, 300, 0.06) }
};

// Calculate ratings (player1 wins)
var updates = ratingService.CalculateMultiPlayerGameRatings(
    participants, 
    winnerId: player1Id);

// Access individual updates
var player1Update = updates[player1Id];
Console.WriteLine($"Player 1: {player1Update.PreviousRating.Rating} → {player1Update.NewRating.Rating}");
Console.WriteLine($"Change: {player1Update.RatingChange:+0.00;-0.00}");
```

### Technical Loss Penalty

```csharp
var culpritRating = new RatingSnapshot(1700, 180, 0.05);

var update = ratingService.ApplyTechnicalLossPenalty(culpritRating);

Console.WriteLine($"Technical Loss Penalty: {update.RatingChange:+0.00;-0.00}");
```

### Integration with Database

The rating system integrates with the database through:

1. **GameParticipant.RatingAtGameTime**: Snapshot of rating before the game
2. **RatingHistory**: Complete audit trail of all rating changes

```csharp
// GameParticipant stores the rating snapshot
participant.RatingAtGameTime = currentRating.Rating;
participant.RatingDeviationAtGameTime = currentRating.RatingDeviation;
participant.VolatilityAtGameTime = currentRating.Volatility;

// RatingHistory records the change
var history = new RatingHistory
{
    LeagueMembershipId = membershipId,
    GameId = gameId,
    PreviousRating = update.PreviousRating.Rating,
    NewRating = update.NewRating.Rating,
    // ... other fields
};
```

## Dependency Injection

Register the rating service in your DI container:

```csharp
services.AddScoped<IRatingService, RatingService>();
```

## Testing

The rating system includes comprehensive tests covering:
- Basic Glicko-2 calculations
- Multi-player game scenarios
- Technical loss penalties
- Edge cases and boundary conditions

Run tests:
```bash
dotnet test src/ScoreBurrow.Rating.Tests
```

## References

- [Glicko-2 Rating System](http://www.glicko.net/glicko/glicko2.pdf) - Mark Glickman's original paper
- [Glicko-2 Example](http://www.glicko.net/glicko/glicko2.pdf) - Step-by-step calculation example

## Implementation Notes

### Why Not Use a NuGet Package?

While there are Glicko-2 implementations available, this custom implementation provides:
- **Multi-player adaptation**: Standard Glicko-2 is for 1v1 matches
- **Technical loss handling**: Custom penalty mechanism
- **Domain integration**: Seamless integration with game entities
- **Full control**: Ability to tune parameters and behavior

### Rating Deviation (RD) Interpretation

- **RD < 100**: Very confident in rating
- **RD 100-200**: Moderately confident
- **RD 200-350**: Less confident (new/inactive players)
- **RD > 350**: Very uncertain

### Volatility Interpretation

- Measures consistency of performance
- Low volatility = consistent results
- High volatility = unpredictable results
- Affects how quickly RD changes

## Future Enhancements

Potential improvements:
- [ ] Rating recalculation tool for historical games
- [ ] Confidence interval calculations
- [ ] Win probability predictions
- [ ] Rating decay for inactive players
- [ ] Leaderboard generation utilities
