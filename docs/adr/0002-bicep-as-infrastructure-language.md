# ADR 0002: Bicep as infrastructure language

## Status

Accepted

## Context

The platform needs a first-party Azure IaC language that supports template validation, what-if planning, policy alignment, and manageable module reuse across commercial and government clouds.

## Decision

Use Bicep for service-pattern entry points, reusable wrapper modules, and orchestration examples. Keep service-pattern contracts pinned to wrapper modules so upstream module choices can change without altering the consumer-facing request model.

## Consequences

The platform benefits from Azure-native tooling and deployment semantics. Contributors must stay current with Bicep linting and module versioning practices. Cross-cloud support remains easier because the same ARM control plane semantics apply to both targeted Azure clouds.
