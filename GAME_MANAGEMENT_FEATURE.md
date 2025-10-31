# Game Management Feature

## Overview

A comprehensive game management system has been added to ScoreBurrow.Web, supporting the complete game lifecycle from creation through completion with integrated rating calculations.

## Implementation Summary

### Components Created

#### 1. Service Layer
- **IGameService** (`src/ScoreBurrow.Web/Services/IGameService.cs`)
  - `CreateGameAsync()` - Creates new game with participants
  - `CompleteGameAsync()` - Finishes game and calculates ratings
  - `ApplyTechnicalLossAsync()` - Applies penalty and restarts game
  - `CancelGameAsync()` - Cancels game without rating changes
  - `GetGameForManagementAsync()` - Retrieves game details for management

- **GameService** (`src/ScoreBurrow.Web/Services/GameService.cs`)
  - Full implementation with rating integration
  - Permission checking via ILeagueService
  - Rating snapshot and calculation via IRatingService

#### 2. Models/DTOs
- **CreateGameRequest** - Game creation payload
- **ParticipantRequest** - Individual participant data
- **GameDetailsDto** - Game details for management
- **ParticipantDto** - Participant view model

#### 3. Pages

##### CreateGame.razor (`/leagues/{leagueId}/game/create`)
Multi-step wizard with 4 stages:

**Step 1: Basic Setup**
- Map name input
- Number of players selection (2-8)
- Player selection from league members

**Step 2: Color Assignment**
- Automatic color assignment
- "Shuffle Colors" button for randomization
- Manual color adjustment via dropdowns
- Position determined by color (Red=1, Blue=2, etc.)

**Step 3: Town, Hero & Gold Trading**
- Town selection per player (required)
- Hero selection filtered by town (optional)
- Bid amount input
- **Automatic gold distribution**:
  - Player 1 bid → split among players 2-4
  - Player 2 bid → split among players 3-4
  - Player 3 bid → goes to player 4
  - Player 4 bid → no distribution
- Real-time gold calculation display

**Step 4: Review & Confirm**
- Summary of all game settings
- Participant details table
- "Start Game" button to create

##### ManageGame.razor (`/leagues/{leagueId}/game/{gameId}/manage`)
Management interface with three action cards:

**Complete Game Card**
- Winner selection dropdown
- Calculates ratings for all participants
- Updates LeagueMembership ratings
- Creates RatingHistory records
- Updates player statistics
- Sets game status to Completed

**Technical Loss Card**
- Culprit selection dropdown
- Applies technical loss penalty to culprit
- Creates new game with same settings
- Culprit receives **-1000 gold penalty**
- Old game marked as Cancelled
- Redirects to new game management

**Cancel Game Card**
- Confirmation modal
- Sets game status to Cancelled
- **No rating changes** applied
- Returns to league page

#### 4. Navigation Updates

**League.razor Updates**
- Added "New Game" button (visible to admins/owners)
- Added "Manage" button for in-progress games in games table
- Status badges displayed properly in table

## Features

### Gold Trading Logic
The system implements the specified gold distribution:
```
Player 1 bid: divided among players 2, 3, 4
Player 2 bid: divided among players 3, 4
Player 3 bid: goes entirely to player 4
Player 4 bid: no distribution
```

Net gold is calculated automatically and displayed in real-time.

### Color Assignment
- Only first N colors available based on player count
- Position is determined by color order
- Shuffle button randomizes assignments
- Manual adjustment supported

### Rating Integration
**On Game Completion:**
1. Snapshots ratings from game start
2. Calls `IRatingService.CalculateMultiPlayerGameRatings()`
3. Winner plays N-1 matches (wins all)
4. Each loser plays 1 match vs winner (loses)
5. Updates all LeagueMembership ratings
6. Creates RatingHistory entries
7. Updates player statistics (games played, wins, favorite town)

**On Technical Loss:**
1. Snapshots culprit's rating
2. Calls `IRatingService.ApplyTechnicalLossPenalty()`
3. Culprit "plays against themselves" and loses
4. Only culprit's rating affected
5. Creates RatingHistory for culprit
6. New game created with -1000 gold penalty for culprit

### Permission Checking
- Only league admins/owners can create/manage games
- Permission verified on every operation
- Uses existing `ILeagueService.IsAdminOrOwnerAsync()`

## Technical Details

### Rating Snapshots
Ratings are captured at game start time:
- `RatingAtGameTime`
- `RatingDeviationAtGameTime`
- `VolatilityAtGameTime`

This ensures calculations use ratings from when the game began, not completion time.

### Game Status Flow
```
InProgress (on creation)
    ↓
Completed (winner selected) OR Cancelled (technical loss/cancel)
```

### Technical Loss Workflow
1. Mark culprit participant as `IsTechnicalLoss = true`
2. Apply rating penalty
3. Cancel current game
4. Create new game:
   - Same map, players, towns, heroes, colors
   - Culprit gets -1000 gold
   - New rating snapshots (culprit has reduced rating)

## Database Impact

No schema changes required - all existing tables support this feature:
- `Games` table for game records
- `GameParticipants` table for participant data
- `RatingHistory` table for rating changes
- `LeagueMemberships` table for current ratings
- `PlayerStatistics` table for stats tracking

## Usage

### Creating a Game
1. Navigate to league page
2. Click "New Game" button (admins/owners only)
3. Complete 4-step wizard
4. Game starts in "In Progress" status

### Managing an In-Progress Game
1. From league page, click "Manage" on in-progress game
2. Choose action:
   - **Complete**: Select winner, ratings calculated
   - **Technical Loss**: Select culprit, game restarted with penalty
   - **Cancel**: Cancel without rating impact

### Viewing Game Details
- All users can view completed/cancelled games
- Game details page shows full participant information

## Build Verification

✅ Build succeeded with 0 warnings, 0 errors
✅ All dependencies properly registered
✅ Rating service integration working
✅ Navigation links properly added

## Future Enhancements

Potential improvements for future iterations:
- [ ] Game history/notes field
- [ ] Export game data
- [ ] Rematch functionality
- [ ] Game templates for common setups
- [ ] Real-time game status updates
- [ ] Mobile-optimized game management interface
