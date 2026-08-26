# Security policy

## Security boundary

Azure Mission Workspace is designed so that AI assistance does not bypass governance. All authentication and authorization checks are enforced server-side, policy evaluation is mandatory, and production-sensitive deployments still require human approval through the platform workflow and Azure DevOps environments.

Key boundary assumptions:

- The AI assistant can help collect requirements and explain results, but it cannot directly authorize a deployment.
- Service-pattern selection, parameter validation, approval checks, and deployment execution are enforced by trusted platform components.
- Evidence and audit records are part of the security posture because they preserve accountability and change traceability.

For more detail, see [docs/security-model.md](docs/security-model.md) and [docs/threat-model.md](docs/threat-model.md).

## Reporting a vulnerability

If you believe you found a vulnerability in this starter repository or the surrounding platform design:

1. Do not post exploit details in a public issue.
2. Email the platform security contact at `security@example.com` with the suspected impact, affected files or components, and reproduction guidance.
3. Expect an acknowledgement within two business days and coordinated next steps for triage.

This repository contains starter artifacts and examples only. Placeholder values must never be replaced with real secrets in source control.
