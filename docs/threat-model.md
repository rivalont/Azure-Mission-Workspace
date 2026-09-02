# Threat model

This document summarizes a STRIDE-style review of the major Azure Mission Workspace control points. The focus is on governed fulfillment, evidence integrity, and preservation of human accountability.

## MCP entry point

- **Spoofing**: An attacker attempts to impersonate a user.  
  **Mitigations**: Entra ID token validation, audience checks, tenant scoping, and short token lifetimes.
- **Tampering**: A caller alters request payloads after review.  
  **Mitigations**: ETags/versioning on deployment requests, server-side schema validation, immutable evidence snapshots.
- **Repudiation**: A user denies having requested a deployment.  
  **Mitigations**: correlation IDs, audit events, signed-in actor identity preserved on every transition.
- **Information disclosure**: Sensitive data leaks through prompts or logs.  
  **Mitigations**: secret references instead of literals, redaction in logs, least-privilege evidence access.
- **Denial of service**: Excessive requests exhaust the MCP server.  
  **Mitigations**: rate limiting, bounded payload sizes, queue-based asynchronous execution.
- **Elevation of privilege**: The assistant attempts to act beyond the caller's rights.  
  **Mitigations**: all authorization enforced server-side; the AI never bypasses role checks.

## Service-pattern catalog tampering

- **Threat**: An attacker modifies descriptors or Bicep to weaken defaults.
- **Mitigations**: pull request review, signed commits where required, pipeline validation, policy checks against normalized plans, and wrapper-module contracts that are reviewed independently from upstream modules.

## Secret exposure

- **Threat**: Literal secrets are embedded in requests, templates, logs, or evidence.
- **Mitigations**: Key Vault secret identifiers only, secure parameter handling, pipeline redaction, and restricted evidence publication.

## Policy bypass attempts

- **Threat**: A caller or contributor crafts parameters that technically validate but evade governance intent.
- **Mitigations**: strict JSON schemas, environment-profile allow-lists, normalized what-if evaluation, mandatory approvals for sensitive environments, and rejection of undeclared parameters.

## Pipeline compromise

- **Threat**: A malicious actor alters pipeline steps or impersonates the deployment identity.
- **Mitigations**: protected pipeline definitions, least-privilege service connections, environment approvals, artifact integrity checks, and separation between validation and deployment identities where practical.

## Evidence tampering

- **Threat**: After deployment, artifacts are modified to hide what happened.
- **Mitigations**: publish evidence as immutable artifacts, store hashes and URIs, record source revision and pipeline execution metadata, and keep audit events separate from mutable conversation state.
