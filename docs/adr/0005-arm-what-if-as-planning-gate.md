# ADR 0005: ARM what-if as planning gate

## Status

Accepted

## Context

Requestors and approvers need a predictable view of intended Azure changes before deployment. Static linting alone cannot describe resource-level create, modify, or delete actions.

## Decision

Use ARM what-if as the primary planning gate whenever authenticated validation is available. Normalize what-if output for policy evaluation and approval review so the same change description can be consumed by humans and automation.

## Consequences

Approvers gain better visibility into pending changes and policy engines can evaluate concrete plans. What-if is not a perfect prediction of runtime behavior, so deployment still requires post-execution evidence and failure handling.
