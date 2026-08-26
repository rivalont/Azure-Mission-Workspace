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

@description('Delegated subnet resource ID for App Service regional VNet integration.')
param subnetResourceId string

@description('Container image reference for the Linux API application.')
param containerImage string

@description('App Service plan SKU.')
param skuName string = 'P1v3'

@description('Health check path exposed by the API.')
param healthCheckPath string = '/healthz'

@description('Minimum worker instance count.')
param minInstanceCount int = 1

@description('Enable zone redundancy for the App Service plan where supported.')
param zoneRedundantPlan bool = false

@description('Additional non-secret application settings.')
param appSettings object = {}

module tags '../../bicep/shared/tags.bicep' = {
  name: 'api-tags'
  params: {
    workloadName: workloadName
    environment: environment
    costCenter: costCenter
    owner: owner
    dataClassification: dataClassification
    additionalTags: {
      servicePatternId: 'internal-web-api'
      diagnosticsWorkspace: last(split(logAnalyticsWorkspaceResourceId, '/'))
    }
  }
}

module naming '../../bicep/shared/naming.bicep' = {
  name: 'api-naming'
  params: {
    prefix: 'amw'
    workloadName: workloadName
    environment: environment
    location: location
    resourceType: 'app'
  }
}

// Equivalent pinned registry reference in a private module registry:
// br:missionworkspace.azurecr.io/bicep/modules/app-service:1.0.0
module appService '../../bicep/modules/app-service.bicep' = {
  name: 'internal-web-api'
  params: {
    location: location
    appName: naming.outputs.primaryName
    planName: '${naming.outputs.primaryName}-plan'
    containerImage: containerImage
    serviceKind: 'api'
    skuName: skuName
    subnetResourceId: subnetResourceId
    logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceId
    tags: tags.outputs.tags
    healthCheckPath: healthCheckPath
    minInstanceCount: minInstanceCount
    zoneRedundantPlan: zoneRedundantPlan
    appSettings: union({
      ASPNETCORE_ENVIRONMENT: environment
      WEBSITE_HEALTHCHECK_MAXPINGFAILURES: '2'
    }, appSettings)
  }
}

output appServiceName string = appService.outputs.appName
output defaultHostName string = appService.outputs.defaultHostName
output principalId string = appService.outputs.principalId
output outboundIpAddresses string = appService.outputs.outboundIpAddresses
