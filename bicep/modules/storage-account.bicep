/*
Module wrapper: pins/abstracts upstream Azure Verified Module choice; MCP/service-pattern contracts
 depend on this wrapper's parameter/output contract, not the upstream module directly.
*/
targetScope = 'resourceGroup'

@description('Azure region for the storage account.')
param location string
@description('Storage account name.')
param storageAccountName string
@description('Replication SKU for the storage account.')
param replicationSku string
@description('Storage account kind.')
param accountKind string = 'StorageV2'
@description('Default blob access tier.')
param accessTier string = 'Hot'
@description('Allow blob public access when explicitly approved.')
param allowBlobPublicAccess bool = false
@description('Enable hierarchical namespace for Data Lake Storage Gen2.')
param enableHierarchicalNamespace bool = false
@description('Subnet resource IDs allowed by network rules.')
param subnetResourceIds array
@description('Log Analytics workspace resource ID for diagnostic settings.')
param logAnalyticsWorkspaceResourceId string
@description('Standard tags applied to all resources.')
param tags object

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: replicationSku
  }
  kind: accountKind
  properties: {
    accessTier: accessTier
    allowBlobPublicAccess: allowBlobPublicAccess
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    isHnsEnabled: enableHierarchicalNamespace
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Disabled'
    supportsHttpsTrafficOnly: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      virtualNetworkRules: [for subnetId in subnetResourceIds: {
        id: subnetId
        action: 'Allow'
      }]
      ipRules: []
    }
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
        file: {
          enabled: true
          keyType: 'Account'
        }
      }
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${storageAccountName}-diagnostics'
  scope: storage
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        category: 'StorageRead'
        enabled: true
      }
      {
        category: 'StorageWrite'
        enabled: true
      }
      {
        category: 'StorageDelete'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
    ]
  }
}

output storageAccountName string = storage.name
output blobEndpoint string = storage.properties.primaryEndpoints.blob
output resourceId string = storage.id
