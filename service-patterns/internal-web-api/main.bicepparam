using 'main.bicep'

// Example template only. Real values are rendered deterministically by the ParameterRenderer at request time.
param workloadName = 'orders-api'
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param environment = readEnvironmentVariable('AMW_ENVIRONMENT', 'dev')
param costCenter = '<cost-center-placeholder>'
param owner = 'team-orders@example.com'
param dataClassification = 'Internal'
param logAnalyticsWorkspaceResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<observability-rg>/providers/Microsoft.OperationalInsights/workspaces/<law-name>'
param subnetResourceId = '/subscriptions/<subscription-id-placeholder>/resourceGroups/<network-rg>/providers/Microsoft.Network/virtualNetworks/<vnet-name>/subnets/<integration-subnet>'
param containerImage = 'missionworkspace.azurecr.io/orders-api:1.0.0'
param skuName = 'P1v3'
param healthCheckPath = '/healthz'
param minInstanceCount = 1
param zoneRedundantPlan = false
param appSettings = {
  Api__BasePath: '/'
  Observability__CorrelationHeader: 'x-correlation-id'
}
