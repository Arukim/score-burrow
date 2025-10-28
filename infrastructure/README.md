# Score Burrow Infrastructure

This directory contains the Azure infrastructure configuration for the Score Burrow application using Bicep templates.

## Architecture Overview

The infrastructure deploys:
- **App Service** (Free tier F1) - Hosts the .NET 8 web application
- **SQL Server & Database** (Basic tier - free tier eligible) - Relational database
- **CosmosDB** (Free tier enabled) - NoSQL database
- **Managed Identity** - For secure, credential-free authentication
- **Firewall Rules** - Network security for database access

## Security Configuration

### Managed Identity
The App Service is configured with a **System-Assigned Managed Identity**, which provides:
- ✅ No credentials stored in code or configuration
- ✅ Automatic credential rotation
- ✅ Azure AD-based authentication
- ✅ Fine-grained access control with RBAC

### SQL Server Security
- **Azure AD Authentication**: Managed Identity is configured as an Azure AD admin
- **Firewall Rules**: 
  - Azure services access enabled (0.0.0.0)
  - App Service outbound IPs whitelisted
  - Public access enabled with IP restrictions
- **TLS 1.2** minimum encryption
- **Basic Tier**: Free tier eligible, 2GB max storage

### CosmosDB Security
- **Built-in Data Contributor Role**: Granted to Managed Identity
- **IP Firewall**: App Service outbound IPs whitelisted
- **Free Tier**: 400 RU/s and 5GB storage free
- **Public access** with IP restrictions

## Prerequisites

1. **Azure CLI** installed and logged in
   ```bash
   az login
   ```

2. **Resource Group** created
   ```bash
   az group create --name score-burrow-rg --location australiaeast
   ```

3. **Strong SQL Admin Password** (replace in parameters.json)
   - Minimum 8 characters
   - Must contain uppercase, lowercase, numbers, and special characters

## Deployment Steps

### 1. Update Parameters

Edit `parameters.json` and set a strong SQL admin password:

```json
{
  "sqlAdminPassword": {
    "value": "YourStrongPassword123!"
  }
}
```

**⚠️ IMPORTANT**: Never commit passwords to source control. Use Azure Key Vault or deployment-time parameters in production.

### 2. Deploy Infrastructure

Run the deployment script:

```bash
cd infrastructure
chmod +x deploy.sh
./deploy.sh
```

Or deploy manually:

```bash
az deployment group create \
  --resource-group score-burrow-rg \
  --template-file main.bicep \
  --parameters parameters.json
```

### 3. Capture Outputs

After deployment, save the output values (you'll need these for application configuration):

```bash
az deployment group show \
  --resource-group score-burrow-rg \
  --name <deployment-name> \
  --query properties.outputs
```

Key outputs:
- `appServiceUrl` - Your application URL
- `appServicePrincipalId` - Managed Identity ID
- `sqlServerFqdn` - SQL Server endpoint
- `sqlConnectionString` - Connection string for SQL with Managed Identity
- `cosmosDbEndpoint` - CosmosDB endpoint

## Post-Deployment Configuration

### 1. Configure App Service Connection Strings

Add connection strings to your App Service using Managed Identity authentication:

#### SQL Server Connection
```bash
az webapp config connection-string set \
  --resource-group score-burrow-rg \
  --name score-burrow-app-dev \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:score-burrow-sql-dev.database.windows.net,1433;Database=ScoreBurrowDb;Authentication=Active Directory Default;"
```

#### CosmosDB Connection
```bash
az webapp config appsettings set \
  --resource-group score-burrow-rg \
  --name score-burrow-app-dev \
  --settings CosmosDb__Endpoint="https://score-burrow-cosmos-dev.documents.azure.com:443/"
```

### 2. Application Code Changes

Update your application to use Managed Identity authentication:

#### SQL Server (in `Program.cs` or `Startup.cs`)
```csharp
// Add NuGet packages:
// - Azure.Identity
// - Microsoft.Data.SqlClient

using Azure.Identity;
using Microsoft.Data.SqlClient;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var credential = new DefaultAzureCredential();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var sqlConnection = new SqlConnection(connectionString);
    sqlConnection.AccessToken = credential.GetToken(
        new Azure.Core.TokenRequestContext(
            new[] { "https://database.windows.net/.default" }
        )).Token;
    
    options.UseSqlServer(sqlConnection);
});
```

#### CosmosDB (in `Program.cs` or `Startup.cs`)
```csharp
// Add NuGet packages:
// - Azure.Identity
// - Microsoft.Azure.Cosmos

using Azure.Identity;
using Microsoft.Azure.Cosmos;

var cosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"];
var credential = new DefaultAzureCredential();

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    return new CosmosClient(cosmosEndpoint, credential);
});
```

### 3. Test Connectivity

After deploying your application:

1. Navigate to your App Service URL
2. Check application logs for any authentication errors
3. Verify database connectivity

To view logs:
```bash
az webapp log tail \
  --resource-group score-burrow-rg \
  --name score-burrow-app-dev
```

## Free Tier Limits

### SQL Server Basic Tier
- ✅ Free tier eligible
- 2 GB maximum database size
- 5 DTU (Database Transaction Units)
- Best for development and small workloads

### CosmosDB Free Tier
- ✅ First 400 RU/s free (per subscription)
- ✅ First 5 GB storage free
- Only one free tier account per subscription
- Ideal for development and testing

### App Service F1 (Free Tier)
- 60 CPU minutes/day
- 1 GB RAM
- 1 GB storage
- No custom domains or SSL
- Goes to sleep after 20 minutes of inactivity

**Note**: You can only have one free tier CosmosDB account per Azure subscription. If you already have one, set `enableFreeTier: false` in the deployment.

## Security Best Practices

### For Production:

1. **Use Azure Key Vault** for SQL admin password
   ```bicep
   param sqlAdminPassword string = keyVault.getSecret('sql-admin-password')
   ```

2. **Enable Private Endpoints** (requires higher SKUs)
   - Removes databases from public internet
   - Uses Azure private networking

3. **Enable Advanced Threat Protection**
   ```bash
   az sql server threat-policy update \
     --resource-group score-burrow-rg \
     --server score-burrow-sql-dev \
     --state Enabled
   ```

4. **Enable Diagnostic Logging**
   ```bash
   az monitor diagnostic-settings create \
     --resource <resource-id> \
     --workspace <log-analytics-workspace-id> \
     --logs '[{"category": "SQLSecurityAuditEvents", "enabled": true}]'
   ```

5. **Rotate Secrets Regularly**
   - SQL admin password should be rotated
   - Consider using temporary access with Managed Identity only

## Troubleshooting

### Connection Issues

1. **Check Managed Identity is enabled**:
   ```bash
   az webapp identity show \
     --resource-group score-burrow-rg \
     --name score-burrow-app-dev
   ```

2. **Verify Firewall Rules**:
   ```bash
   az sql server firewall-rule list \
     --resource-group score-burrow-rg \
     --server score-burrow-sql-dev
   ```

3. **Check CosmosDB IP Rules**:
   ```bash
   az cosmosdb show \
     --resource-group score-burrow-rg \
     --name score-burrow-cosmos-dev \
     --query "ipRules"
   ```

### Authentication Issues

1. **Verify RBAC assignments**:
   ```bash
   # For SQL
   az role assignment list \
     --assignee <principalId> \
     --scope <sql-database-id>
   
   # For CosmosDB
   az cosmosdb sql role assignment list \
     --resource-group score-burrow-rg \
     --account-name score-burrow-cosmos-dev
   ```

2. **Test Managed Identity locally** using Azure CLI:
   ```bash
   az login --identity
   ```

## Clean Up

To delete all resources:

```bash
az group delete --name score-burrow-rg --yes --no-wait
```

## Module Structure

```
infrastructure/
├── main.bicep                      # Main orchestration template
├── parameters.json                 # Deployment parameters
├── deploy.sh                       # Deployment script
└── modules/
    ├── appServicePlan.bicep       # App Service Plan
    ├── appService.bicep           # App Service with Managed Identity
    ├── sqlServer.bicep            # SQL Server with security config
    ├── cosmosDb.bicep             # CosmosDB with security config
    └── sqlServerSecurity.bicep    # (Legacy - for existing resources)
```

## Support

For issues or questions:
- Check Azure Portal for deployment errors
- Review App Service logs
- Verify all firewall rules are correctly configured
