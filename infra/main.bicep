targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary Azure region for resources')
param location string

@description('Region for Azure Static Web Apps (not available in uksouth)')
param staticWebAppLocation string = 'eastus2'

@description('Region for PostgreSQL Flexible Server (blocked for new servers in some US/EU regions)')
param postgresLocation string = 'uksouth'

@description('Id of the principal to grant Key Vault secret access')
param principalId string = ''

@description('Principal type of the deployer (User or ServicePrincipal)')
param principalType string = 'ServicePrincipal'

@secure()
@description('PostgreSQL administrator password')
param databasePassword string

@secure()
@description('JWT signing key. azd may generate 15 chars; Bicep pads to 32+ if needed.')
param jwtSigningKey string

var resourceSuffix = take(uniqueString(subscription().id, environmentName, location), 6)
var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources './modules/resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    name: environmentName
    location: location
    staticWebAppLocation: staticWebAppLocation
    postgresLocation: postgresLocation
    tags: tags
    resourceSuffix: resourceSuffix
    principalId: principalId
    principalType: principalType
    databasePassword: databasePassword
    jwtSigningKey: jwtSigningKey
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_LOCATION string = location
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_KEY_VAULT_NAME string = resources.outputs.AZURE_KEY_VAULT_NAME
output AZURE_KEY_VAULT_ENDPOINT string = resources.outputs.AZURE_KEY_VAULT_ENDPOINT
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
output API_URI string = resources.outputs.API_URI
output WEB_URI string = resources.outputs.WEB_URI
output POSTGRES_FQDN string = resources.outputs.POSTGRES_FQDN
