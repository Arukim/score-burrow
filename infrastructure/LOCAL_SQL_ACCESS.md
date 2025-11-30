# Accessing Deployed Azure SQL Database from Local Machine

This guide explains how to connect to your deployed Azure SQL Database from your local development machine for debugging purposes.

## Prerequisites

1. Azure CLI installed and logged in (`az login`)
2. SQL Server client tools (Azure Data Studio, SSMS, sqlcmd, or VS Code with SQL Server extension)
3. Access to `infrastructure/parameters.local.json` for connection credentials

## Step 1: Add Your Local IP to SQL Server Firewall

The Azure SQL Server firewall currently only allows connections from Azure services and the App Service. You need to add your local machine's public IP address using Azure CLI:

```bash
# Get your current public IP
MY_IP=$(curl -s https://api.ipify.org)

# Set your deployment details
SQL_SERVER_NAME="score-burrow-sql-dev"
RESOURCE_GROUP="score-burrow-rg"

# Add firewall rule for your IP
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "LocalDevelopment-$(whoami)" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP

echo "Firewall rule added for IP: $MY_IP"
```

## Step 2: Get Connection Information

From your `infrastructure/parameters.local.json` file:

- **Server**: `score-burrow-sql-dev.database.windows.net`
- **Database**: `ScoreBurrowDbDev`
- **Admin Username**: Value from `sqlAdminLogin` parameter
- **Admin Password**: Value from `sqlAdminPassword` parameter

## Step 3: Connect to the Database

### Using Azure Data Studio

1. Open Azure Data Studio
2. Click **New Connection**
3. Fill in the connection details:
   - **Connection type**: Microsoft SQL Server
   - **Server**: `score-burrow-sql-dev.database.windows.net`
   - **Authentication type**: SQL Login
   - **User name**: (from parameters.local.json)
   - **Password**: (from parameters.local.json)
   - **Database**: `ScoreBurrowDbDev`
   - **Encrypt**: True
   - **Trust server certificate**: False
4. Click **Connect**

### Using sqlcmd Command Line

```bash
sqlcmd -S score-burrow-sql-dev.database.windows.net \
  -d ScoreBurrowDb \
  -U <username-from-parameters> \
  -P '<password-from-parameters>' \
  -N -C
```

### Using Connection String in .NET Application

Update your `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:score-burrow-sql-dev.database.windows.net,1433;Initial Catalog=ScoreBurrowDbDev;Persist Security Info=False;User ID=<username>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

Replace `<username>` and `<password>` with values from `parameters.local.json`.

## Step 4: Running the Application Locally with Azure SQL

1. Update your local connection string as shown above
2. The database already has the schema from migrations (deployed version has run migrations)
3. Run your application locally:

```bash
cd src/ScoreBurrow.Web
dotnet run
```

The application will now connect to the Azure SQL Database instead of a local database.

## Step 5: Verify Connection

Test the connection by running a simple query:

```sql
-- List all tables
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Check AspNetUsers table (from Identity)
SELECT COUNT(*) as UserCount FROM AspNetUsers;
```

## Troubleshooting

### Connection Timeout or Cannot Connect

1. **Verify firewall rule**: Your public IP might have changed
   ```bash
   curl https://api.ipify.org
   ```
   If it's different, delete the old rule and create a new one

2. **Check if SQL Server is paused**: Serverless databases auto-pause after 60 minutes of inactivity
   - First connection after pause may take 30-60 seconds to resume
   - Try connecting again after waiting a minute

3. **Verify credentials**: Double-check username and password from `parameters.local.json`

### Certificate Trust Issues

If you get SSL/TLS certificate errors:
- Ensure `Encrypt=True` and `TrustServerCertificate=False` in connection string
- Update your SQL client tools to the latest version

## Cleaning Up

When you're done with local development, remove your firewall rule:

```bash
az sql server firewall-rule delete \
  --resource-group score-burrow-rg \
  --server score-burrow-sql-dev \
  --name "LocalDevelopment-$(whoami)"
```

## Security Best Practices

1. **Limit IP Access**: Only add your specific IP, not broad ranges
2. **Use Strong Passwords**: Keep the admin password secure and never commit it to version control
3. **Remove Firewall Rules**: Delete firewall rules when not actively debugging
4. **Rotate Credentials**: Regularly update the SQL admin password
5. **Consider Azure AD Authentication**: For production, use Azure AD instead of SQL authentication
