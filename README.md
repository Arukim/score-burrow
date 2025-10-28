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

After infrastructure is deployed:

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
     --resource-group rg-score-burrow-dev \
     --name score-burrow-app-dev \
     --src-path ../deploy.zip \
     --type zip
   ```

## Technology Stack

- **Frontend**: Blazor Server (ASP.NET Core)
- **Backend**: .NET 6
- **Infrastructure**: Azure App Service (Linux)
- **IaC**: Azure Bicep

## Features (To Be Implemented)

- 🎮 Track game scores for multiple players
- 📊 Calculate winners and rankings
- 🏆 View game history and statistics
- 📈 Player performance tracking
- 🎯 Multiple game type support

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
