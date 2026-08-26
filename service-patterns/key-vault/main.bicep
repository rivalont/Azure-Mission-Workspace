targetScope = 'resourceGroup'

@description('Short workload identifier used to generate resource names.')
param workloadName string
@description('Azure region for the deployment.')
param location string = resourceGroup().location
@description('Environment tag value such as dev, test, stg, or prod.')
param environment string
@description('Cost allocation tag value required by platform governance.')
param costCenter string
@description('Owning team or email alias.')
param owner string
@description('Data sensitivity classification tag.')
param dataClassification string
@description('Resource ID of the Log Analytics workspace for diagnostics.')
param logAnalyticsWorkspaceResourceId string
@description('Entra tenant ID used by the vault.')
param tenantId string
@description('Approved subnet resource ID for network restrictions.')
param subnetResourceId string
@description('Soft delete retention period in days.')
param softDeleteRetentionInDays int = 90
@description('Enable purge protection.')
param purgeProtectionEnabled bool = true
@description('Vault SKU.')
param keyVaultSku string = 'standard'
@description('Object IDs granted data-plane secret user access.')
param allowedObjectIds array = []

module tags '../../bicep/shared/tags.bicep' = {
  name: 'kv-tags'
  params: {
    workloadName: workloadName
    environment: environment
    costCenter: costCenter
    owner: owner
    dataClassification: dataClassification
    additionalTags: {
      servicePatternId: 'key-vault'
      diagnosticsWorkspace: last(split(logAnalyticsWorkspaceResourceId, '/'))
    }
  }
}

module naming '../../bicep/shared/naming.bicep' = {
  name: 'kv-naming'
  params: {
    prefix: 'amw'
    workloadName: workloadName
    environment: environment
    location: location
    resourceType: 'kv'
  }
}

// Equivalent pinned registry reference in a private module registry:
// br:missionworkspace.azurecr.io/bicep/modules/key-vault:1.0.0
module vault '../../bicep/modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    keyVaultName: take(naming.outputs.primaryName, 24)
    tenantId: tenantId
    subnetResourceId: subnetResourceId
    logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceId
    softDeleteRetentionInDays: softDeleteRetentionInDays
    purgeProtectionEnabled: purgeProtectionEnabled
    keyVaultSku: keyVaultSku
    allowedObjectIds: allowedObjectIds
    tags: tags.outputs.tags
  }
}

output keyVaultName string = vault.outputs.keyVaultName
output vaultUri string = vault.outputs.vaultUri
output resourceId string = vault.outputs.resourceId
