# Authoring a new service pattern

A service pattern is the governed unit of infrastructure fulfillment in Azure Mission Workspace. Authors add patterns to the catalog only when the platform team is willing to review, support, and evolve the lifecycle contract over time.

## Required artifacts

Each service pattern folder must contain:

1. `service-pattern.yaml` descriptor with metadata, governance requirements, security defaults, module references, approval rules, and documented outputs.
2. `input-schema.json` describing the request parameters accepted by the pattern.
3. `main.bicep` as the pattern entry point.
4. `main.bicepparam` example parameter file showing placeholders and deterministic rendering expectations.
5. `README.md` describing purpose, inputs, outputs, security defaults, and the starter-template disclaimer.
6. `tests/README.md` and `tests/sample-request.json` capturing expected validation coverage and a representative request payload.

## Descriptor conventions

- Use a stable `id` in kebab-case.
- Keep `version` semantically versioned; increment major versions when request contracts or outputs break compatibility.
- `deploymentStrategy` must reflect how the deployment pipeline should execute the compiled template.
- `supportedClouds`, `supportedEnvironmentTypes`, and `supportedRegions` should be explicit. Omit speculative claims.
- `requiredInputs` are request-time parameters; `secretInputs` must only reference Key Vault secret identifiers, never literal values.
- `moduleReferences` should point at the wrapper module contract and use a pinned registry version even when the starter repository uses relative local paths.

## Bicep conventions

- Use `targetScope = 'resourceGroup'` for service-pattern entry points unless a reviewed exception exists.
- Add `@description` for every parameter and output.
- Consume wrapper modules from `bicep/modules` so upstream module selection can change without breaking the service-pattern contract.
- Apply secure defaults such as HTTPS only, minimum TLS 1.2, disabled public network access where supported, and mandatory diagnostics.
- Prefer deterministic naming helpers and standard tags from `bicep/shared`.

## Input schema guidance

- Set `additionalProperties` to `false` unless the pattern intentionally accepts open-ended structures.
- Keep enum lists aligned with the descriptor and environment profiles.
- Model secret references as URI-shaped strings that point to Key Vault secrets.
- Validate placeholder-friendly values where appropriate, but keep the schema strict enough to prevent accidental drift.

## Tests and validation

At minimum, a new pattern should be covered by:

- descriptor schema validation
- JSON schema validation for request parameters
- Bicep build and lint checks
- sample request verification against the deployment-request schema
- policy evaluation scenarios for required security controls

The .NET validation suite and Azure DevOps pipelines execute the authoritative automation. Pattern authors should still ensure their local changes are coherent before review.

## Review and approval to join the catalog

1. Open a pull request with the new pattern artifacts and a rationale for platform support.
2. Include security and operations reviewers from the owning platform team.
3. Demonstrate that the descriptor, request schema, and Bicep outputs match.
4. Show how the pattern aligns with required tags, diagnostics, and approval policy.
5. Document expected consumers, lifecycle ownership, and deprecation approach.

A pattern should only enter the catalog after reviewers agree that its contract is stable enough for governed reuse.
