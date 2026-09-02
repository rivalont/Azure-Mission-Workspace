/*
Module wrapper: pins/abstracts upstream Azure Verified Module choice; MCP/service-pattern contracts
 depend on this wrapper's parameter/output contract, not the upstream module directly.
*/
targetScope = 'resourceGroup'

@description('Azure region for the Key Vault.')
param location string
@description('Name of the Key Vault.')
param keyVaultName string
@description('Entra tenant ID.')
param tenantId string
@description('Approved subnet resource ID for network ACLs or private endpoint alignment.')
param subnetResourceId string
@description('Log Analytics workspace resource ID for diagnostic settings.')
param logAnalyticsWorkspaceResourceId string
@description('Soft delete retention period in days.')
param softDeleteRetentionInDays int = 90
@description('Enable purge protection.')
param purgeProtectionEnabled bool = true
@description('Vault SKU.')
param keyVaultSku string = 'standard'
@description('Entra object IDs granted Key Vault Secrets User RBAC assignments.')
param allowedObjectIds array = []
@description('Standard tags applied to all resources.')
param tags object

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: subnetResourceId
        }
      ]
      ipRules: []
    }
    publicNetworkAccess: 'Disabled'
    purgeProtectionEnabled: purgeProtectionEnabled
    softDeleteRetentionInDays: softDeleteRetentionInDays
    sku: {
      family: 'A'
      name: toUpper(keyVaultSku)
    }
    tenantId: tenantId
  }
}

resource roleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for objectId in allowedObjectIds: {
  name: guid(vault.id, objectId, 'Key Vault Secrets User')
  scope: vault
  properties: {
    principalId: objectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}]

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${keyVaultName}-diagnostics'
  scope: vault
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output keyVaultName string = vault.name
output vaultUri string = vault.properties.vaultUri
output resourceId string = vault.id
