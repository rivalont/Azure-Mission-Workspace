using 'main.bicep'

// Example template only. Real values are rendered deterministically by the ParameterRenderer at request time.
param workloadName = 'ordersdata'
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param environment = readEnvironmentVariable('AMW_ENVIRONMENT', 'dev')
param costCenter = '<cost-center-placeholder>'
param owner = 'team-orders@example.com'
param dataClassification = 'Confidential'
param logAnalyticsWorkspaceResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<observability-rg>/providers/Microsoft.OperationalInsights/workspaces/<law-name>'
param replicationSku = 'Standard_LRS'
param subnetResourceIds = [
  '/subscriptions/<subscription-id-placeholder>/resourceGroups/<network-rg>/providers/Microsoft.Network/virtualNetworks/<vnet-name>/subnets/<private-endpoint-subnet>'
]
param accountKind = 'StorageV2'
param accessTier = 'Hot'
param allowBlobPublicAccess = false
param enableHierarchicalNamespace = false
