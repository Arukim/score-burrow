# ScoreBurrow.Data

This project contains the database schema and Entity Framework Core configuration for the ScoreBurrow application.

## Overview

ScoreBurrow.Data is a separate .NET class library that defines the database structure for tracking Heroes of Might and Magic 3 game scores within leagues.

## Database Schema

### Core Entities

#### League
Represents a competitive league where players compete.
- Supports active/inactive states
- Owner-based access control
- Full audit trail (CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)

#### LeagueMembership
Links users to leagues with their in-game nicknames and roles.
- **Supports unregistered players** - UserId is nullable to allow tracking players before they create accounts
- PlayerNickname: In-game name (e.g., "dominator")
- PlayerDisplayName: Display name for unregistered users
- Role: Owner, Admin, or Member
- **Glicko-2 Rating System**: Stores rating, rating deviation, and volatility
- Full audit trail

#### Game
Records individual game matches.
- StartTime/EndTime tracking (no calculated duration)
- Status: InProgress, Completed, Cancelled
- MapName, Notes
- Winner tracking
- Full audit trail

#### GameParticipant
Links league members to games with detailed game information.
- Town selection (faction)
- Hero selection
- PlayerColor: Uses HoMM3 8-color scheme (Red, Blue, Tan, Green, Orange, Purple, Teal, Pink)
- Position: Final placement (1st, 2nd, 3rd, etc.)
- IsWinner flag
- IsTechnicalLoss: Tracks losses to neutral mobs
- GoldTrade: Simplified trade tracking (positive = received, negative = gave)
- Full audit trail

#### Town (Reference Data)
The 9 factions from Heroes of Might and Magic 3:
- Castle, Rampart, Tower, Inferno, Necropolis, Dungeon, Stronghold, Fortress, Conflux

#### Hero (Reference Data)
All 144 heroes from HoMM3, linked to their respective towns.
- Hero name, class (e.g., Knight, Wizard, Barbarian)
- Town affiliation

#### PlayerStatistics
Aggregate statistics per league membership.
- Games played/won, technical losses
- Win rate, average position
- Favorite town and hero (most frequently played)
- LastUpdated timestamp

## Features

### Audit Trail
All user-input entities implement `IAuditableEntity`:
- CreatedBy, CreatedOn
- ModifiedBy, ModifiedOn

The DbContext automatically populates these fields on SaveChanges.

### Glicko-2 Rating System
LeagueMembership stores three parameters:
- Glicko2Rating (default: 1500)
- Glicko2RatingDeviation (default: 350)
- Glicko2Volatility (default: 0.06)

### Seed Data
- **9 Towns**: All HoMM3 factions pre-populated
- **144 Heroes**: Complete hero roster from HoMM3 with town affiliations

## Configuration

### Entity Configurations
Each entity has a dedicated configuration class in `Configurations/`:
- Defines constraints, indexes, and relationships
- Configures string lengths and precision
- Sets up cascade behaviors

### Key Indexes
- League: OwnerId, IsActive
- LeagueMembership: LeagueId, UserId, unique (LeagueId, PlayerNickname)
- Game: LeagueId, StartTime, Status
- GameParticipant: GameId, LeagueMembershipId, unique (GameId, PlayerColor)
- Town/Hero: Name (unique)

### Relationships
- **Cascade Delete**: League → Memberships, Games → Participants
- **Restrict**: Participant → Town/Hero (preserve reference data)
- **Set Null**: Game.WinnerId, Statistics favorites

## Migrations

Migrations are stored in the `Migrations/` folder.

### Creating Migrations
```bash
cd src/ScoreBurrow.Data
dotnet ef migrations add MigrationName
```

### Applying Migrations
Migrations should be applied automatically by the web application on startup.

## Usage

### DbContext Configuration
The `ScoreBurrowDbContext` requires:
- Connection string via `DbContextOptions`
- Optional `currentUserId` parameter for audit trail

Example:
```csharp
services.AddDbContext<ScoreBurrowDbContext>((serviceProvider, options) =>
{
    var httpContext = serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
    var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    options.UseSqlServer(connectionString);
    return new ScoreBurrowDbContext(options, userId);
});
```

## Notes

- All datetime fields use UTC
- Player colors match HoMM3's 8-color system for authenticity
- Unregistered players can be tracked and linked to accounts later via UserId
- Technical losses (to neutral mobs) are tracked separately for detailed statistics
