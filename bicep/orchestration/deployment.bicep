// Illustrative orchestration example only.
// A real worker or pipeline can choose a pattern dynamically after catalog resolution.
targetScope = 'subscription'

@description('Name of the resource group that will host the workload deployment.')
param resourceGroupName string
@description('Azure region for the resource group and service pattern deployment.')
param location string
@description('Governed workload short name.')
param workloadName string
@description('Container image for the sample internal web API.')
param containerImage string
@description('Environment short code.')
param environment string = 'dev'
@description('Cost center tag value.')
param costCenter string
@description('Owning team or alias.')
param owner string
@description('Data classification tag value.')
param dataClassification string = 'Internal'
@description('Log Analytics workspace resource ID.')
param logAnalyticsWorkspaceResourceId string
@description('Delegated App Service subnet resource ID.')
param subnetResourceId string

resource workloadRg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    managedBy: 'AzureMissionWorkspace'
    environment: environment
  }
}

module internalWebApi '../../service-patterns/internal-web-api/main.bicep' = {
  name: 'deploy-internal-web-api'
  scope: resourceGroup(resourceGroupName)
  params: {
    workloadName: workloadName
    location: location
    environment: environment
    costCenter: costCenter
    owner: owner
    dataClassification: dataClassification
    logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceId
    subnetResourceId: subnetResourceId
    containerImage: containerImage
  }
  dependsOn: [
    workloadRg
  ]
}

output resourceGroupId string = workloadRg.id
output appServiceName string = internalWebApi.outputs.appServiceName
