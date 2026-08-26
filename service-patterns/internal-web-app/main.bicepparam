using 'main.bicep'

// Example template only. Real values are rendered deterministically by the ParameterRenderer at request time.
param workloadName = 'hr-portal'
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus2')
param environment = readEnvironmentVariable('AMW_ENVIRONMENT', 'test')
param costCenter = '<cost-center-placeholder>'
param owner = 'team-hr@example.com'
param dataClassification = 'Internal'
param logAnalyticsWorkspaceResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<observability-rg>/providers/Microsoft.OperationalInsights/workspaces/<law-name>'
param subnetResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<network-rg>/providers/Microsoft.Network/virtualNetworks/<vnet-name>/subnets/<integration-subnet>'
param containerImage = 'missionworkspace.azurecr.io/hr-portal:1.0.0'
param skuName = 'P1v3'
param healthCheckPath = '/'
param minInstanceCount = 1
param startupCommand = ''
param appSettings = {
  Web__BasePath: '/'
  FeatureFlags__SelfServiceProfile: 'true'
}
