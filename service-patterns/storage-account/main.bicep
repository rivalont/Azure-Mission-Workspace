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
@description('Storage replication SKU.')
param replicationSku string
@description('Approved subnet resource IDs for network restrictions.')
param subnetResourceIds array
@description('Storage account kind.')
param accountKind string = 'StorageV2'
@description('Default blob access tier.')
param accessTier string = 'Hot'
@description('Allow public blob access when explicitly approved.')
param allowBlobPublicAccess bool = false
@description('Enable hierarchical namespace for analytics workloads.')
param enableHierarchicalNamespace bool = false

module tags '../../bicep/shared/tags.bicep' = {
  name: 'storage-tags'
  params: {
    workloadName: workloadName
    environment: environment
    costCenter: costCenter
    owner: owner
    dataClassification: dataClassification
    additionalTags: {
      servicePatternId: 'storage-account'
      diagnosticsWorkspace: last(split(logAnalyticsWorkspaceResourceId, '/'))
    }
  }
}

module naming '../../bicep/shared/naming.bicep' = {
  name: 'storage-naming'
  params: {
    prefix: 'amw'
    workloadName: workloadName
    environment: environment
    location: location
    resourceType: 'stg'
  }
}

// Equivalent pinned registry reference in a private module registry:
// br:missionworkspace.azurecr.io/bicep/modules/storage-account:1.0.0
module storage '../../bicep/modules/storage-account.bicep' = {
  name: 'storage-account'
  params: {
    location: location
    storageAccountName: take(replace(naming.outputs.primaryName, '-', ''), 24)
    replicationSku: replicationSku
    accountKind: accountKind
    accessTier: accessTier
    allowBlobPublicAccess: allowBlobPublicAccess
    enableHierarchicalNamespace: enableHierarchicalNamespace
    subnetResourceIds: subnetResourceIds
    logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceId
    tags: tags.outputs.tags
  }
}

output storageAccountName string = storage.outputs.storageAccountName
output blobEndpoint string = storage.outputs.blobEndpoint
output resourceId string = storage.outputs.resourceId
