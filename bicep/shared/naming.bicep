targetScope = 'resourceGroup'

@description('Short prefix identifying the platform.')
param prefix string = 'amw'
@description('Workload name included in the generated resource name.')
param workloadName string
@description('Environment short code.')
param environment string
@description('Azure location used as part of the uniqueness seed.')
param location string
@description('Short resource type token such as app, stg, or kv.')
param resourceType string
@description('Optional suffix for multiple instances.')
param suffix string = ''

var uniqueSegment = substring(uniqueString(resourceGroup().id, workloadName, environment, resourceType, location), 0, 5)
var optionalSuffix = empty(suffix) ? '' : '-${suffix}'

output primaryName string = '${prefix}-${workloadName}-${environment}-${resourceType}-${uniqueSegment}${optionalSuffix}'
output compactName string = replace('${prefix}${workloadName}${environment}${resourceType}${uniqueSegment}${suffix}', '-', '')
