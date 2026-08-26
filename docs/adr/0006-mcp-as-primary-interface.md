# ADR 0006: MCP as primary interface

## Status

Accepted

## Context

The platform is intended to meet users where they already work with AI-assisted tooling while preserving deterministic back-end governance.

## Decision

Use Model Context Protocol as the primary request interface. The MCP layer captures intent, gathers structured parameters, and surfaces evidence and approval state, while all privileged actions remain server-side.

## Consequences

The experience becomes conversational and discoverable without granting the AI direct deployment authority. The platform must guard carefully against prompt injection, over-trust in generated output, and confusion between suggestions and approved execution.
