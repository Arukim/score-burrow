# Score Burrow

Game score calculator built with Blazor Server and Azure infrastructure.

## Project Structure

```
score-burrow/
├── infrastructure/          # Azure infrastructure as code
│   ├── main.bicep          # Main Bicep template
│   ├── parameters.json     # Deployment parameters
│   ├── deploy.sh           # Deployment script
│   └── modules/            # Bicep modules
│       ├── appService.bicep
│       └── appServicePlan.bicep
├── src/                    # Source code
│   ├── ScoreBurrow.sln    # .NET solution
│   └── ScoreBurrow.Web/   # Blazor Server web application
└── README.md
```

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/) (recommended)

## Local Development

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

### Running Tests (when added)

```bash
cd src
dotnet test
```

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

#### Option 1: Automated Deployment (Recommended)

After infrastructure is deployed, use the automated deployment script:

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

#### Option 2: Manual Deployment

If you prefer manual deployment:

1. Build and publish the application:
   ```bash
   cd src/ScoreBurrow.Web
   dotnet publish -c Release -o ./publish
   ```

2. Create a deployment package:
   ```bash
   cd publish
   zip -r ../deploy.zip .
   ```

3. Deploy to Azure App Service:
   ```bash
   az webapp deploy \
     --resource-group score-burrow-rg \
     --name <app-service-name> \
     --src-path ../deploy.zip \
     --type zip
   ```

#### Monitoring and Troubleshooting

View live logs:
```bash
az webapp log tail \
  --resource-group score-burrow-rg \
  --name <app-service-name>
```

View deployment history:
```bash
az webapp deployment list \
  --resource-group score-burrow-rg \
  --name <app-service-name>
```

## Technology Stack

- **Frontend**: Blazor Server (ASP.NET Core)
- **Backend**: .NET 6
- **Infrastructure**: Azure App Service (Linux)
- **IaC**: Azure Bicep

## Features

### Implemented
- 🎮 Track game scores for multiple players
- 📊 Calculate winners and rankings using Glicko-2 rating system
- 🏆 View game history and comprehensive statistics
- 📈 Advanced player performance tracking:
  - **Color-Adjusted Win Rate**: Player win rates normalized by expected color performance
  - **Color Distribution Analysis**: Breakdown of colors played by game size
  - Town and hero performance statistics
  - Rating history visualization
- 🎯 League management with multiple game type support
- 🔐 Authentication and authorization
- 📦 Data import from CSV files

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

### To Be Implemented
- 🌐 Multi-language support
- 📱 Mobile-optimized interface
- 📧 Email notifications for game updates
- 🏅 Achievement system

## Development Roadmap

- [x] Project setup and infrastructure
- [x] Basic Blazor Server application
- [ ] Score entry interface
- [ ] Game management
- [ ] Player management
- [ ] Score calculation logic
- [ ] Statistics and reporting
- [ ] Data persistence (database)
- [ ] Authentication and authorization

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

See LICENSE file for details.

# High level design

- Hosted in Azure
- Written in .NET
- Using Azure free-tier as much as possible
- Infrastructure as a code using Bicep templates

Resources to deploy
- Web App to host ASP.NET core web app
- Azure SQL free tier
- Azure Cosmos DB free tier
- Configure connectivity between resources
