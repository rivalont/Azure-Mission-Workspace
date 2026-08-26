# ADR 0003: Approved service patterns

## Status

Accepted

## Context

Allowing unrestricted infrastructure authoring through MCP would undermine governance, supportability, and policy consistency. The platform needs a catalog of reviewed building blocks with known ownership and lifecycle expectations.

## Decision

Expose only approved service patterns through the fulfillment workflow. Each pattern must include a descriptor, input schema, Bicep entry point, documentation, and explicit ownership metadata before it is added to the catalog.

## Consequences

Governance and support quality improve because the platform fulfills only known-good contracts. The trade-off is slower onboarding for new infrastructure shapes, so the authoring process must remain documented and predictable.
