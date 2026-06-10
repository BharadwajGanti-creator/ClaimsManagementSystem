@description('Base name used to derive resource names. Lowercase letters/numbers.')
param namePrefix string = 'claims'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Name of the (already created) Azure Container Registry to pull from.')
param acrName string

@description('Container image repository:tag inside the ACR, e.g. claims-api:abc123.')
param containerImage string

@description('Azure SQL administrator login.')
param sqlAdminLogin string

@secure()
@description('Azure SQL administrator password.')
param sqlAdminPassword string

@secure()
@description('JWT signing key (>= 32 chars).')
param jwtSigningKey string

@secure()
@description('Seeded admin account password for first login.')
param seedAdminPassword string

var sqlServerName = '${namePrefix}-sql-${uniqueString(resourceGroup().id)}'
var sqlDbName = 'ClaimsDb'
var planName = '${namePrefix}-plan'
var webAppName = '${namePrefix}-api-${uniqueString(resourceGroup().id)}'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

// Allow other Azure services (the Web App) to reach the SQL server.
resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true // required for Linux
  }
}

var acrCreds = acr.listCredentials()
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDbName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${acr.properties.loginServer}/${containerImage}'
      alwaysOn: true
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'WEBSITES_PORT', value: '8080' }
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'DOCKER_REGISTRY_SERVER_URL', value: 'https://${acr.properties.loginServer}' }
        { name: 'DOCKER_REGISTRY_SERVER_USERNAME', value: acrCreds.username }
        { name: 'DOCKER_REGISTRY_SERVER_PASSWORD', value: acrCreds.passwords[0].value }
        { name: 'ConnectionStrings__ClaimsDb', value: sqlConnectionString }
        { name: 'Jwt__Issuer', value: 'ClaimsManagementSystem' }
        { name: 'Jwt__Audience', value: 'ClaimsManagementSystem.Clients' }
        { name: 'Jwt__SigningKey', value: jwtSigningKey }
        { name: 'Jwt__AccessTokenMinutes', value: '60' }
        { name: 'Seed__AdminEmail', value: 'admin@claims.local' }
        { name: 'Seed__AdminPassword', value: seedAdminPassword }
        { name: 'Storage__LocalRootPath', value: '/home/claim-documents' }
      ]
    }
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
