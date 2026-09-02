# Internal Web API

This service pattern deploys a private Linux App Service intended for HTTP APIs consumed inside the enterprise network boundary. It standardizes naming, tags, managed identity, diagnostics, and VNet integration while leaving workload-specific application settings to request-time rendering.

## Required inputs

- `workloadName`, `location`, and `environment` drive deterministic naming and policy scoping.
- `costCenter`, `owner`, and `dataClassification` satisfy mandatory governance tags.
- `logAnalyticsWorkspaceResourceId` sends platform and application diagnostics to a central workspace.
- `subnetResourceId` enables regional VNet integration.
- `containerImage` selects the workload artifact.

## Optional inputs

Optional inputs allow teams to tune SKU, health checks, minimum capacity, zone redundancy, and non-secret application settings.

## Outputs

- App Service name
- Default hostname
- Managed identity principal ID
- Outbound IP addresses

## Security defaults

- System-assigned managed identity enabled
- HTTPS only
- Minimum TLS 1.2
- Public network access disabled
- Diagnostic settings required

## Starter template disclaimer

This example is a governed starter template. It is not a universal statement of production readiness, and teams must validate workload-specific availability, capacity, data handling, and recovery requirements before use in higher environments.
