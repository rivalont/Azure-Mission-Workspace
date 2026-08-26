# ADR 0004: Azure DevOps as deployment executor

## Status

Accepted

## Context

The platform requires a deterministic execution engine with approvals, reusable templates, evidence publication, and compatibility with existing enterprise controls.

## Decision

Use Azure DevOps pipelines as the validation and deployment executor. The application and worker layers prepare requests and evidence, but Azure DevOps remains the path that performs validate, what-if, policy evaluation, and actual deployment steps.

## Consequences

This leverages familiar controls such as environments, approvals, and artifacts. The platform becomes dependent on Azure DevOps availability and governance maturity, so pipeline definitions must be treated as part of the trusted computing base.
