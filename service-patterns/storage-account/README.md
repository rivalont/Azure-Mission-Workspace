# Storage Account

This service pattern provisions a governed Azure Storage account for internal platform scenarios. It emphasizes secure transport, deterministic naming, restricted network access, diagnostic emission, and mandatory governance tags.

## Inputs

The required inputs cover workload naming, standard ownership tags, location, diagnostics workspace, a replication SKU, and the subnets permitted by network controls. Optional inputs tune account kind, access tier, hierarchical namespace, and public blob access behavior.

## Outputs

- Storage account name
- Blob endpoint URI
- Resource ID

## Security defaults

- HTTPS traffic only
- Minimum TLS 1.2
- Public network access disabled
- Default network action deny
- Blob public access disabled unless explicitly set

## Starter template disclaimer

This artifact is an example governed starter. It does not claim universal production readiness or compliance certification; teams must validate data retention, resiliency, backup, and workload-specific access patterns before promotion.
