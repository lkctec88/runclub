targetScope = 'resourceGroup'

param name string
param location string
param staticWebAppLocation string
param postgresLocation string
param tags object
param resourceSuffix string
param principalId string
param principalType string = ''

@secure()
param databasePassword string

@secure()
param jwtSigningKey string

// azd secretOrRandomPassword is 15 chars (5 lower + 5 upper + 5 numeric).
// The API requires 32+ in Production. Keep a long existing key; pad a short one.
var jwtSigningKeyResolved = length(jwtSigningKey) >= 32
  ? jwtSigningKey
  : '${jwtSigningKey}${uniqueString(resourceGroup().id, 'jwt-pad-1')}${uniqueString(resourceGroup().id, 'jwt-pad-2')}'

var deployerPrincipalType = empty(principalType) ? 'ServicePrincipal' : principalType

// Key Vault: 3-24 chars, alphanumeric + hyphen
var keyVaultName = take(toLower('kv${replace(name, '-', '')}${resourceSuffix}'), 24)
// ACR: 5-50 alphanumeric only
var acrName = toLower('craz0${resourceSuffix}')
var postgresName = take(toLower('psql-${name}-${resourceSuffix}'), 63)
var logName = take('log-${name}-${resourceSuffix}', 63)
var appiName = take('appi-${name}-${resourceSuffix}', 63)
var caeName = take('cae-${name}-${resourceSuffix}', 60)
var apiAppName = take(toLower('ca-api-${resourceSuffix}'), 32)
var swaName = take('stapp-${name}-${resourceSuffix}', 63)
var identityName = take('id-api-${name}-${resourceSuffix}', 128)
var postgresAdminLogin = 'runclubadmin'
var postgresDatabaseName = 'runclub'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appiName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    // Container Apps resolves Key Vault secret refs from Azure infrastructure IPs.
    // A Deny firewall (e.g. only a home IP) blocks jwt-signing-key / postgres-connection.
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Disabled'
  }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

resource kvSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    )
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource kvDeployerSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(keyVault.id, principalId, 'KeyVaultSecretsOfficer')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
    )
    principalId: principalId
    principalType: deployerPrincipalType
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: postgresName
  location: postgresLocation
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: databasePassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource postgresDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: postgresDatabaseName
}

resource postgresFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

var postgresConnectionString = 'Host=${postgres.properties.fullyQualifiedDomainName};Port=5432;Database=${postgresDatabaseName};Username=${postgresAdminLogin};Password=${databasePassword};Ssl Mode=Require;Trust Server Certificate=true'

resource postgresConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-connection'
  properties: {
    value: postgresConnectionString
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-signing-key'
  properties: {
    value: jwtSigningKeyResolved
  }
}

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: caeName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: swaName
  location: staticWebAppLocation
  tags: union(tags, { 'azd-service-name': 'web' })
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    allowConfigFileUpdates: true
  }
}

module apiApp './container-app.bicep' = {
  name: 'api-container-app'
  params: {
    name: apiAppName
    location: location
    tags: union(tags, { 'azd-service-name': 'api' })
    containerAppsEnvironmentId: containerAppsEnv.id
    userAssignedIdentityId: identity.id
    acrLoginServer: acr.properties.loginServer
    keyVaultUri: keyVault.properties.vaultUri
    applicationInsightsConnectionString: applicationInsights.properties.ConnectionString
    corsOrigin: 'https://${staticWebApp.properties.defaultHostname}'
  }
  dependsOn: [
    postgresConnectionSecret
    jwtSecret
    kvSecretsUser
    acrPullRoleUser
  ]
}

module acrPullRole './acr-pull-role.bicep' = {
  name: 'acr-pull-role'
  params: {
    acrName: acr.name
    principalId: apiApp.outputs.systemAssignedMIPrincipalId
  }
}

module acrPullRoleUser './acr-pull-role.bicep' = {
  name: 'acr-pull-role-user'
  params: {
    acrName: acr.name
    principalId: identity.properties.principalId
  }
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = acr.properties.loginServer
output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.properties.vaultUri
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalytics.id
output API_URI string = apiApp.outputs.fqdn
output WEB_URI string = 'https://${staticWebApp.properties.defaultHostname}'
output POSTGRES_FQDN string = postgres.properties.fullyQualifiedDomainName
