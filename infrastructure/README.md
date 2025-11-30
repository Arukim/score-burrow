# Score Burrow Infrastructure

This directory contains the Azure infrastructure configuration for the Score Burrow application using Bicep templates.

## Architecture Overview

The infrastructure deploys:
- **App Service** (Free tier F1) - Hosts the .NET 8 web application
- **SQL Server & Database** (Serverless Gen5 with free tier) - Relational database
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
- **Serverless Gen5**: Free tier with 2 vCores, 32GB max storage, auto-pause after 60 minutes


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

### 1. Create Local Parameters (Recommended)

To keep your secrets safe and out of source control, create a local parameters file:

```bash
cd infrastructure
cp parameters.json parameters.local.json
```

Then edit `parameters.local.json` with your actual values:

```json
{
  "sqlAdminPassword": {
    "value": "YourStrongPassword123!"
  }
}
```

**✅ `parameters.local.json` is git-ignored and will NOT be committed**

The deployment script automatically uses `parameters.local.json` if it exists, otherwise it falls back to `parameters.json`.

**Alternative**: Edit `parameters.json` directly, but be careful not to commit sensitive values to source control.

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

## Post-Deployment Configuration

### 1. Configure App Service Connection Strings

Add connection strings to your App Service using Managed Identity authentication:

#### SQL Server Connection
```bash
az webapp config connection-string set \
  --resource-group score-burrow-rg \
  --name score-burrow-app-dev \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:score-burrow-sql-dev.database.windows.net,1433;Database=ScoreBurrowDbDev;Authentication=Active Directory Default;"
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

### SQL Server Serverless (General Purpose Gen5)
- ✅ Free tier eligible
- 32 GB maximum database size
- 2 vCores capacity
- 0.5 vCores minimum when active
- Auto-pause after 60 minutes of inactivity
- Best for development and intermittent workloads

### App Service F1 (Free Tier)
- 60 CPU minutes/day
- 1 GB RAM
- 1 GB storage
- No custom domains or SSL
- Goes to sleep after 20 minutes of inactivity

**Note**: Azure SQL Database free tier provides 32GB storage and 100,000 vCore seconds of compute per month, which is ideal for development and testing.

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
