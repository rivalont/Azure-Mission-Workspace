# ADR 0001: Modular monolith

## Status

Accepted

## Context

Azure Mission Workspace needs clear domain boundaries, strong testability, and a deployable shape that stays operationally simple while the platform contract is still evolving. A distributed microservice split would add coordination overhead before the bounded contexts and scale characteristics are proven.

## Decision

Adopt a modular monolith for the primary application. Keep MCP handling, application orchestration, service-pattern catalog logic, policy evaluation, and evidence workflows in well-defined internal modules with explicit contracts.

## Consequences

The codebase can evolve quickly with simpler deployment and debugging. The team must enforce boundaries through design and tests because process boundaries will not do that automatically. Selective extraction remains possible later if a module proves to require independent scaling or isolation.
