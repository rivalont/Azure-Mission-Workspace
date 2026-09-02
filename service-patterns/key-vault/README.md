# Key Vault

This service pattern provides a governed starter for Azure Key Vault deployments that need private access, RBAC authorization, purge protection, and evidence-friendly diagnostics. It is intended for centrally reviewed secret and key hosting scenarios rather than ad hoc vault creation.

## Inputs

Required inputs capture governance tags, location, tenant, diagnostics workspace, and the approved subnet reference used by the network design. Optional inputs tune SKU, purge protection behavior, soft delete retention, and RBAC principal assignments.

## Outputs

- Key Vault name
- Vault URI
- Resource ID

## Security defaults

- Public network access disabled
- RBAC authorization enabled
- Soft delete retained for 90 days by default
- Purge protection enabled by default
- Diagnostic settings required

## Starter template disclaimer

This example is a governed starter template only. It does not guarantee workload-specific key management readiness, residency suitability, or certification outcomes; consumers must complete their own operational and security reviews.
