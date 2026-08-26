using 'main.bicep'

// Example template only. Real values are rendered deterministically by the ParameterRenderer at request time.
param workloadName = 'shared-secrets'
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param environment = readEnvironmentVariable('AMW_ENVIRONMENT', 'test')
param costCenter = '<cost-center-placeholder>'
param owner = 'team-platform@example.com'
param dataClassification = 'Restricted'
param logAnalyticsWorkspaceResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<observability-rg>/providers/Microsoft.OperationalInsights/workspaces/<law-name>'
param tenantId = '<tenant-id-placeholder>'
param subnetResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<network-rg>/providers/Microsoft.Network/virtualNetworks/<vnet-name>/subnets/<private-endpoint-subnet>'
param softDeleteRetentionInDays = 90
param purgeProtectionEnabled = true
param keyVaultSku = 'standard'
param allowedObjectIds = [
  '00000000-0000-0000-0000-000000000001'
]
