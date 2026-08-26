# ADR 0007: Human identity preservation

## Status

Accepted

## Context

A governed deployment platform must preserve the identity of the real human actors who requested, approved, and reviewed changes. Delegating those actions to an assistant would weaken accountability.

## Decision

Record and preserve human identity at each workflow transition. The assistant may help collect inputs or explain validation results, but it does not become the approver, requestor, or deployment authority.

## Consequences

Audit quality and separation of duties improve. Additional implementation work is required to carry object IDs, correlation IDs, and approval metadata through every pipeline and evidence artifact.
