/*
Module wrapper: pins/abstracts upstream Azure Verified Module choice; MCP/service-pattern contracts
 depend on this wrapper's parameter/output contract, not the upstream module directly.
*/
targetScope = 'resourceGroup'

@description('Azure region for the App Service resources.')
param location string
@description('Name of the App Service site.')
param appName string
@description('Name of the App Service plan.')
param planName string
@description('Container image reference for the Linux workload.')
param containerImage string
@description('Workload kind used for tagging and defaults.')
@allowed([ 'api', 'webapp' ])
param serviceKind string
@description('App Service plan SKU.')
param skuName string = 'P1v3'
@description('Regional VNet integration subnet resource ID.')
param subnetResourceId string
@description('Log Analytics workspace resource ID for diagnostic settings.')
param logAnalyticsWorkspaceResourceId string
@description('Standard tags applied to all resources.')
param tags object
@description('Health check path.')
param healthCheckPath string = '/healthz'
@description('Minimum elastic worker count.')
param minInstanceCount int = 1
@description('Optional startup command.')
param startupCommand string = ''
@description('Enables zone redundancy on the App Service plan when supported.')
param zoneRedundantPlan bool = false
@description('Additional non-secret application settings.')
param appSettings object = {}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  tags: tags
  sku: {
    name: skuName
    tier: contains(skuName, 'P') ? 'PremiumV3' : 'IsolatedV2'
    size: skuName
    capacity: minInstanceCount
  }
  properties: {
    reserved: true
    zoneRedundant: zoneRedundantPlan
  }
}

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    publicNetworkAccess: 'Disabled'
    virtualNetworkSubnetId: subnetResourceId
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerImage}'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      http20Enabled: true
      healthCheckPath: healthCheckPath
      minimumElasticInstanceCount: minInstanceCount
      appCommandLine: empty(startupCommand) ? null : startupCommand
      appSettings: concat([
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'SERVICE_KIND'
          value: serviceKind
        }
      ], [for key in objectKeys(appSettings): {
        name: key
        value: string(appSettings[key])
      }])
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${appName}-diagnostics'
  scope: site
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAuditLogs'
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

output appName string = site.name
output defaultHostName string = site.properties.defaultHostName
output principalId string = site.identity.principalId
output outboundIpAddresses string = site.properties.outboundIpAddresses
output planResourceId string = plan.id
