@description('CosmosDB account name')
param cosmosDbAccountName string

@description('Location for CosmosDB')
param location string

@description('App Service Principal ID (Managed Identity)')
param appServicePrincipalId string

@description('App Service Outbound IP Addresses (comma-separated)')
param appServiceOutboundIps string

@description('CosmosDB default consistency level')
@allowed([
  'Eventual'
  'ConsistentPrefix'
  'Session'
  'BoundedStaleness'
  'Strong'
])
param defaultConsistencyLevel string = 'Session'

@description('Database name')
param databaseName string = 'ScoreBurrowDb'

@description('Enable free tier (first account per subscription)')
param enableFreeTier bool = true

// Parse IP addresses and create IP rules
var ipAddressList = split(appServiceOutboundIps, ',')
var ipRules = [for ip in ipAddressList: {
  ipAddressOrRange: trim(ip)
}]

// Create CosmosDB account with free tier
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: enableFreeTier
    consistencyPolicy: {
      defaultConsistencyLevel: defaultConsistencyLevel
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    ipRules: ipRules
    isVirtualNetworkFilterEnabled: false
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    publicNetworkAccess: 'Enabled'
    capabilities: []
  }
}

// Create database with minimal throughput (400 RU/s is minimum for manual provisioning)
resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  name: databaseName
  parent: cosmosDbAccount
  properties: {
    resource: {
      id: databaseName
    }
    options: {
      throughput: 400 // Minimum throughput (covered by free tier: 400 RU/s free)
    }
  }
}

// Role assignment: Cosmos DB Built-in Data Contributor
// Role definition ID: 00000000-0000-0000-0000-000000000002
var cosmosDbDataContributorRoleId = resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', cosmosDbAccountName, '00000000-0000-0000-0000-000000000002')

resource cosmosDbRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmosDbAccount.id, appServicePrincipalId, 'DataContributor')
  parent: cosmosDbAccount
  properties: {
    roleDefinitionId: cosmosDbDataContributorRoleId
    principalId: appServicePrincipalId
    scope: cosmosDbAccount.id
  }
}

output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbAccountId string = cosmosDbAccount.id
output databaseName string = database.name
