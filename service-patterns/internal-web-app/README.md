# Internal Web App

This service pattern provides a governed starter for internal browser-based applications hosted on Linux App Service. It is designed for workloads that need managed identity, secure ingress defaults, centralized logging, and consistent metadata across development through production environments.

## Inputs

The pattern requires standard governance tags, target location, an integrated subnet, diagnostics workspace, and a container image. Optional inputs let teams adjust scale, health checks, startup behavior, and non-secret application configuration.

## Outputs

- App Service name
- Default hostname
- Managed identity principal ID
- Outbound IP addresses

## Security defaults

- HTTPS only with minimum TLS 1.2
- Public network access disabled
- System-assigned managed identity enabled
- Diagnostics emitted to a central Log Analytics workspace

## Starter template disclaimer

This repository content is a safe starter template. It is not a universal production-ready pattern, and implementing teams remain responsible for workload-specific threat modeling, performance validation, and operational readiness checks.
