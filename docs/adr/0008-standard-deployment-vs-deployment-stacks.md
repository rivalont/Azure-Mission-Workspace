# ADR 0008: Standard deployment vs deployment stacks

## Status

Accepted

## Context

Not every infrastructure shape benefits equally from the same deployment mechanism. Some resources are well served by standard ARM deployments, while others benefit from the lifecycle controls offered by deployment stacks.

## Decision

Support both standard ARM template deployment and deployment stacks, with the chosen strategy declared in the service-pattern descriptor. Validation, approvals, and evidence requirements stay consistent regardless of strategy.

## Consequences

The platform gains flexibility without forcing a single lifecycle model on all patterns. Pipeline logic becomes slightly more complex because strategy selection must remain explicit, reviewable, and evidence-backed.
