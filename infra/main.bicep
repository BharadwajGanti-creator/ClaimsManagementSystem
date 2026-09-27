@description('Base name used to derive resource names.')
param namePrefix string = 'claims'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Full container image reference, e.g. ghcr.io/owner/repo:sha (must be a public image).')
param containerImage string

@secure()
@minLength(32)
@description('JWT signing key (at least 32 UTF-8 bytes).')
param jwtSigningKey string

@secure()
@minLength(12)
@description('Seeded admin password for first login (at least 12 characters).')
param seedAdminPassword string

var envName = '${namePrefix}-env'
var appName = '${namePrefix}-api'

// Container Apps managed environment (Consumption). No Log Analytics is attached,
// which avoids Log Analytics ingestion; total cost depends on usage.
resource managedEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {}
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      secrets: [
        { name: 'jwt-signing-key', value: jwtSigningKey }
        { name: 'seed-admin-password', value: seedAdminPassword }
      ]
    }
    template: {
      containers: [
        {
          name: 'claims-api'
          image: containerImage
          // Demo-only bootstrap: each ephemeral replica starts with an empty DB.
          command: ['/bin/sh', '-c']
          args: ['dotnet Claims.API.dll --migrate-database && exec dotnet Claims.API.dll']
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'Database__Provider', value: 'Sqlite' }
            { name: 'ConnectionStrings__ClaimsDb', value: 'Data Source=/app/data/claims.db' }
            { name: 'Jwt__Issuer', value: 'ClaimsManagementSystem' }
            { name: 'Jwt__Audience', value: 'ClaimsManagementSystem.Clients' }
            { name: 'Jwt__AccessTokenMinutes', value: '60' }
            { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
            { name: 'Seed__AdminEmail', value: 'admin@claims.local' }
            { name: 'Seed__Enabled', value: 'true' }
            { name: 'Seed__AdminPassword', secretRef: 'seed-admin-password' }
            { name: 'Storage__LocalRootPath', value: '/app/claim-documents' }
          ]
        }
      ]
      scale: {
        // Scale to zero when idle => no compute cost between requests.
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output appUrl string = 'https://${app.properties.configuration.ingress.fqdn}'
output appName string = app.name
