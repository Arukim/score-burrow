targetScope = 'resourceGroup'

@description('Name of the application')
param appName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('App Service Plan SKU')
@allowed([
  'F1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1v2'
  'P2v2'
  'P3v2'
])
param appServicePlanSku string = 'F1'

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('SQL Server administrator login')
param sqlAdminLogin string

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('SQL Database name')
param sqlDatabaseName string = 'ScoreBurrowDb'

@description('CosmosDB database name')
param cosmosDbDatabaseName string = 'ScoreBurrowDb'

var appServicePlanName = '${appName}-plan-${environment}'
var appServiceName = '${appName}-app-${environment}'
var sqlServerName = '${appName}-sql-${environment}'
var cosmosDbAccountName = '${appName}-cosmos-${environment}'

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlanDeployment'
  params: {
    appServicePlanName: appServicePlanName
    location: location
    sku: appServicePlanSku
  }
}

module appService 'modules/appService.bicep' = {
  name: 'appServiceDeployment'
  params: {
    appServiceName: appServiceName
    location: location
    appServicePlanId: appServicePlan.outputs.appServicePlanId
  }
}

module sqlServer 'modules/sqlServer.bicep' = {
  name: 'sqlServerDeployment'
  params: {
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    location: location
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    appServicePrincipalId: appService.outputs.appServicePrincipalId
    appServiceOutboundIps: appService.outputs.appServicePossibleOutboundIpAddresses
  }
}

// Configure app settings with connection string after both resources are created
resource appServiceConfig 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${appServiceName}/appsettings'
  properties: {
    ConnectionStrings__DefaultConnection: sqlServer.outputs.sqlConnectionString
  }
}

module cosmosDb 'modules/cosmosDb.bicep' = {
  name: 'cosmosDbDeployment'
  params: {
    cosmosDbAccountName: cosmosDbAccountName
    location: location
    databaseName: cosmosDbDatabaseName
    appServicePrincipalId: appService.outputs.appServicePrincipalId
    appServiceOutboundIps: appService.outputs.appServicePossibleOutboundIpAddresses
    enableFreeTier: true
  }
}

output appServiceUrl string = appService.outputs.appServiceUrl
output appServiceName string = appServiceName
output appServicePrincipalId string = appService.outputs.appServicePrincipalId
output sqlServerFqdn string = sqlServer.outputs.sqlServerFqdn
output sqlDatabaseName string = sqlServer.outputs.sqlDatabaseName
output sqlConnectionString string = sqlServer.outputs.sqlConnectionString
output cosmosDbEndpoint string = cosmosDb.outputs.cosmosDbEndpoint
output cosmosDbDatabaseName string = cosmosDb.outputs.databaseName
