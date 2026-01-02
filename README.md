# Score Burrow

A comprehensive league management system for Heroes of Might and Magic 3 multiplayer games, featuring Glicko-2 rating calculations, player statistics, and game tracking.

## Project Structure

```
score-burrow/
├── infrastructure/                    # Azure infrastructure as code
│   ├── main.bicep                    # Main Bicep template
│   ├── parameters.json               # Deployment parameters
│   ├── deploy.sh                     # Infrastructure deployment script
│   ├── LOCAL_SQL_ACCESS.md           # Guide for local SQL Server access
│   ├── README.md                     # Infrastructure documentation
│   └── modules/                      # Bicep modules
│       ├── appService.bicep          # App Service configuration
│       ├── appServicePlan.bicep      # App Service Plan configuration
│       └── sqlServer.bicep           # SQL Server and Database configuration
├── src/                              # Source code
│   ├── ScoreBurrow.sln              # .NET solution file
│   ├── ScoreBurrow.Web/             # Blazor Server web application
│   │   ├── Pages/                   # Razor pages and components
│   │   │   ├── Account/            # Authentication pages
│   │   │   ├── Leagues/            # League management pages
│   │   │   └── KnowledgeBase/      # HoMM3 reference data (towns, heroes)
│   │   ├── Services/               # Application services
│   │   ├── Models/                 # DTOs and view models
│   │   └── Shared/                 # Shared Razor components
│   ├── ScoreBurrow.Data/           # Database layer (EF Core)
│   │   ├── Entities/               # Domain entities
│   │   ├── Configurations/         # EF Core entity configurations
│   │   ├── Enums/                  # Domain enums
│   │   ├── Migrations/             # EF Core migrations
│   │   └── ScoreBurrowDbContext.cs # Database context
│   ├── ScoreBurrow.Rating/         # Glicko-2 rating system library
│   │   ├── Core/                   # Rating algorithm implementation
│   │   ├── Models/                 # Rating models
│   │   └── Services/               # Rating calculation services
│   ├── ScoreBurrow.DataImport/     # CSV import console application
│   │   ├── Models/                 # Import models
│   │   ├── Services/               # Import services
│   │   └── Program.cs              # CLI entry point
│   └── ScoreBurrow.DataImport.Tests/ # Unit tests for import functionality
├── add-migration.sh                # Helper script for creating migrations
├── update-database.sh              # Helper script for applying migrations
├── deploy-app.sh                   # Application deployment script
├── AUTHENTICATION.md               # Authentication setup guide
├── GAME_MANAGEMENT_FEATURE.md      # Game management feature documentation
└── README.md                       # This file
```

## Overview

Score Burrow is a specialized web application for managing Heroes of Might and Magic 3 competitive leagues. It provides:

- **League Management**: Create and manage competitive leagues with multiple players
- **Game Tracking**: Record game results with HoMM3-specific details (towns, heroes, player colors)
- **Rating System**: Glicko-2 rating algorithm adapted for multiplayer games
- **Player Statistics**: Track wins, losses, favorite factions, and performance metrics
- **Authentication**: Secure user registration and login with ASP.NET Core Identity
- **Data Import**: Import historical game data from CSV files

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express for local development)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (for Azure deployment)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended)

## Local Development

### Database Setup

1. Install SQL Server Express (if not already installed)

2. Update the connection string in `src/ScoreBurrow.Web/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=ScoreBurrow;Integrated Security=true;TrustServerCertificate=true;"
     }
   }
   ```

3. Apply database migrations:
   ```bash
   cd src/ScoreBurrow.Web
   dotnet ef database update --project ../ScoreBurrow.Data
   ```

   Or use the helper script:
   ```bash
   ./update-database.sh
   ```

### Running the Application

1. Navigate to the web project directory:
   ```bash
   cd src/ScoreBurrow.Web
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to:
   - https://localhost:7XXX (check the console output for the exact port)
   - or http://localhost:5XXX

### Building the Solution

```bash
cd src
dotnet build
```

### Running Tests

```bash
cd src
dotnet test
```

## Key Features

### Implemented Features

- ✅ **User Authentication**: Registration, login, and secure account management
- ✅ **League Management**: 
  - Create leagues with custom names and descriptions
  - Owner-based access control
  - Invite players to leagues
  - Role management (Owner, Admin, Member)
- ✅ **Game Tracking**:
  - Record games with detailed HoMM3 data (towns, heroes, 8-color player system)
  - Winner tracking and position recording
  - Technical loss tracking (losses to neutral mobs)
  - Gold trade tracking
  - Map name and notes
  - Town pool selection in game creation wizard
- ✅ **Glicko-2 Rating System**:
  - Multi-player game adaptation (1 winner vs N-1 losers)
  - Rating, Rating Deviation, and Volatility tracking
  - Technical loss penalty system
  - Complete rating history audit trail
- ✅ **Advanced Player Statistics**:
  - Games played, wins, losses
  - Win rate and average position
  - Favorite town and hero analytics
  - Current rating display
  - **Color-Adjusted Win Rate**: Player win rates normalized by expected color performance
  - **Color Distribution Analysis**: Breakdown of colors played by game size
  - Town and hero performance statistics
  - Rating history visualization
- ✅ **HoMM3 Reference Data**:
  - 12 towns/factions (Castle, Rampart, Tower, Inferno, Necropolis, Dungeon, Stronghold, Fortress, Conflux, Cove, Factory, Bulwark)
  - 161 heroes with town affiliations
  - 8-color player system (Red, Blue, Tan, Green, Orange, Purple, Teal, Pink)
- ✅ **Data Import Tool**:
  - CSV import for historical games
  - Automatic date backtracking with Sunday placement
  - Player resolution and league membership creation
  - Dry-run mode for validation
- ✅ **Responsive UI**: Blazor Server with Bootstrap styling

### Statistics Features

#### Color-Weighted Performance
The system analyzes league-wide color statistics (365-day window) to calculate:
- **Expected Win Rate by Color**: Each color's performance in different game sizes
- **Color-Adjusted Win Rate**: Player performance weighted by the difficulty of colors played
  - Formula: `Sum(isWinner ? (1.0 / expectedColorWinRate) : 0) / totalGames`
  - Example: Winning with a color that has 25% expected win rate counts more than winning with a 40% win rate color

#### Color Distribution
Shows the distribution of colors a player has played, broken down by game size:
- Percentage of games played with each color
- Visual progress bars for easy interpretation
- Minimum 3 games required per game size to display statistics

## Database Schema

The application uses Entity Framework Core with SQL Server. Key entities include:

- **ApplicationUser**: ASP.NET Core Identity user
- **League**: Competitive league container
- **LeagueMembership**: Links users to leagues with ratings and roles
- **Game**: Individual game records with timing and status
- **GameParticipant**: Player participation in games with HoMM3 details
- **Town**: Reference data for HoMM3 factions
- **Hero**: Reference data for HoMM3 heroes
- **PlayerStatistics**: Aggregate statistics per league membership
- **RatingHistory**: Audit trail of all rating changes

See [ScoreBurrow.Data README](src/ScoreBurrow.Data/README.md) for detailed schema documentation.

## Rating System

Score Burrow implements the Glicko-2 rating system adapted for multiplayer games:

- **Default Rating**: 1500
- **Default Rating Deviation**: 350
- **Default Volatility**: 0.06
- **Multi-player Logic**: Winner plays N-1 virtual matches against each loser
- **Technical Loss Penalty**: Players causing technical losses play against themselves and lose

See [ScoreBurrow.Rating README](src/ScoreBurrow.Rating/README.md) for detailed rating system documentation.

## Data Import

Import historical game data from CSV files using the DataImport console application:

```bash
dotnet run --project src/ScoreBurrow.DataImport -- \
  --csv "data/games.csv" \
  --league-id "your-league-guid" \
  --dry-run
```

See [ScoreBurrow.DataImport README](src/ScoreBurrow.DataImport/README.md) for detailed import documentation.

## Azure Deployment

### Prerequisites

1. Install Azure CLI
2. Login to Azure:
   ```bash
   az login
   ```

### Deploy Infrastructure

1. Navigate to the infrastructure directory:
   ```bash
   cd infrastructure
   ```

2. Review and modify parameters if needed:
   - Edit `parameters.json` to customize:
     - `appName`: Application name (must be globally unique)
     - `location`: Azure region (default: australiaeast)
     - `appServicePlanSku`: Service plan tier (F1 for free tier)
     - `environment`: Environment name (dev/staging/prod)
     - `sqlAdministratorLogin`: SQL admin username
     - `sqlAdministratorLoginPassword`: SQL admin password

3. Run the deployment script:
   ```bash
   ./deploy.sh
   ```

   This script will:
   - Validate your Azure login
   - Create a resource group
   - Validate the Bicep template
   - Deploy the infrastructure
   - Output the App Service URL

### Deploy Application Code

Use the automated deployment script:

```bash
./deploy-app.sh
```

This script will automatically:
- Check prerequisites (Azure CLI, .NET SDK)
- Verify Azure login and resource group existence
- Clean previous build artifacts
- Build and publish the application
- Create a deployment package
- Deploy to Azure App Service
- Display the App Service URL
- Clean up temporary files

See [Infrastructure README](infrastructure/README.md) for detailed deployment documentation.

## Technology Stack

- **Frontend**: Blazor Server (ASP.NET Core 8.0)
- **Backend**: .NET 8, C# 12
- **Database**: SQL Server (Azure SQL for production)
- **ORM**: Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity
- **Infrastructure**: Azure App Service (Linux), Azure SQL Database
- **IaC**: Azure Bicep
- **Rating Algorithm**: Glicko-2 (custom implementation)

## Development Roadmap

### Completed
- [x] Project setup and infrastructure
- [x] Azure Bicep templates for deployment
- [x] SQL Server database with EF Core
- [x] ASP.NET Core Identity authentication
- [x] Database schema for leagues, games, and players
- [x] Glicko-2 rating system implementation
- [x] League creation and management
- [x] Game creation with HoMM3 specifics
- [x] Player statistics tracking
- [x] CSV import tool for historical data
- [x] Rating history audit trail
- [x] Knowledge base pages for towns and heroes
- [x] Color-adjusted win rate statistics
- [x] Color distribution analysis by game size
- [x] Town pool selection in game wizard
- [x] Added Bulwark town and 17 heroes (Chieftain and Elder classes)

### In Progress
- [ ] Game editing and management UI
- [ ] Enhanced player profile pages
- [ ] Leaderboards and rankings

### Planned
- [ ] Real-time game updates with SignalR
- [ ] Mobile-responsive improvements
- [ ] Export functionality (CSV, PDF)
- [ ] Admin dashboard
- [ ] API for external integrations
- [ ] Rating recalculation tools
- [ ] Multi-language support
- [ ] Email notifications for game updates
- [ ] Achievement system

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Create a Pull Request

## License

See LICENSE file for details.

## High Level Design

- **Hosting**: Azure App Service (Linux)
- **Runtime**: .NET 8
- **Database**: Azure SQL Database (Serverless free tier for development)
- **Infrastructure**: Bicep templates for Infrastructure as Code
- **Authentication**: ASP.NET Core Identity with SQL Server storage
- **Cost Optimization**: Using Azure free-tier resources where possible

### Azure Resources
- **App Service**: Linux-based hosting for the Blazor Server application
- **App Service Plan**: Free tier (F1) for development, scalable for production
- **Azure SQL Database**: Serverless GP_S_Gen5 tier with auto-pause
- **Managed Identity**: For secure App Service to SQL Server authentication
- **Firewall Rules**: Configured for App Service outbound IPs
