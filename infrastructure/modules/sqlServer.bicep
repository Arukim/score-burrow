@description('SQL Server name')
param sqlServerName string

@description('SQL Database name')
param sqlDatabaseName string

@description('Location for SQL Server')
param location string

@description('SQL Server administrator login')
param administratorLogin string

@description('SQL Server administrator password')
@secure()
param administratorLoginPassword string

@description('App Service Principal ID (Managed Identity)')
param appServicePrincipalId string

@description('App Service Outbound IP Addresses (comma-separated)')
param appServiceOutboundIps string

@description('SQL Database SKU - Free tier')
param databaseSku object = {
  name: 'GP_S_Gen5'
  tier: 'GeneralPurpose'
  family: 'Gen5'
  capacity: 2
}

// Create SQL Server
resource sqlServer 'Microsoft.Sql/servers@2024-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Create SQL Database with Free Tier (Serverless)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2024-05-01-preview' = {
  name: sqlDatabaseName
  parent: sqlServer
  location: location
  sku: databaseSku
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB (free tier limit)
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    autoPauseDelay: 60 // Auto-pause after 60 minutes of inactivity (free tier feature)
    minCapacity: json('0.5') // Minimum vCores when active (free tier)
    requestedBackupStorageRedundancy: 'Local'
    isLedgerOn: false
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    availabilityZone: 'NoPreference'
  }
}

// Firewall rule to allow Azure services and resources
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2024-05-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Firewall rules for App Service outbound IPs
var ipAddressList = split(appServiceOutboundIps, ',')

resource appServiceFirewallRules 'Microsoft.Sql/servers/firewallRules@2024-05-01-preview' = [for (ip, index) in ipAddressList: {
  name: 'AppService-IP-${index}'
  parent: sqlServer
  properties: {
    startIpAddress: trim(ip)
    endIpAddress: trim(ip)
  }
}]

// Configure Azure AD authentication
resource sqlServerAdministrator 'Microsoft.Sql/servers/administrators@2024-05-01-preview' = {
  name: 'ActiveDirectory'
  parent: sqlServer
  properties: {
    administratorType: 'ActiveDirectory'
    login: 'AppServiceManagedIdentity'
    sid: appServicePrincipalId
    tenantId: subscription().tenantId
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output sqlDatabaseId string = sqlDatabase.id
output sqlConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Default;'
