targetScope = 'resourceGroup'

@description('Logical workload identifier.')
param workloadName string
@description('Environment tag value.')
param environment string
@description('Cost allocation code.')
param costCenter string
@description('Owning team or contact alias.')
param owner string
@description('Data classification label.')
param dataClassification string
@description('Additional tags to merge into the standard set.')
param additionalTags object = {}

output tags object = union({
  workload: workloadName
  environment: environment
  costCenter: costCenter
  owner: owner
  dataClassification: dataClassification
  managedBy: 'AzureMissionWorkspace'
}, additionalTags)
