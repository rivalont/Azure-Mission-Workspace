# Contributing

Thank you for contributing to Azure Mission Workspace.

## Adding a service pattern

1. Create a new folder under `service-patterns/`.
2. Add `service-pattern.yaml`, `input-schema.json`, `main.bicep`, `main.bicepparam`, `README.md`, and `tests/` artifacts.
3. Align the descriptor with `schemas/service-pattern.schema.json`.
4. Use wrapper modules from `bicep/modules` instead of binding directly to upstream module implementations.
5. Add or update example environment profile compatibility and policy documentation when needed.

See [docs/service-pattern-authoring.md](docs/service-pattern-authoring.md) for the full authoring guide.

## Coding conventions

For the .NET solution maintained elsewhere in this repository:

- Nullable reference types should remain enabled.
- Treat warnings as errors.
- Use central package management for NuGet dependencies.
- Keep domain contracts explicit and versioned.

For infrastructure artifacts in this repository:

- Prefer deterministic naming and shared tag helpers.
- Use managed identity and secure defaults where the Azure resource supports them.
- Never commit real secrets; use placeholders or Key Vault secret identifiers.
- Keep JSON schemas strict with `additionalProperties: false` unless extensibility is intentional.

## Running tests

Run the existing validation commands that apply to your change set:

- `.NET` tests for descriptor and workflow behavior through the repository test projects.
- Bicep restore, build, format, and lint checks through the Azure DevOps templates in `pipelines/`.
- JSON schema syntax validation for new schemas, environment profiles, and sample requests.

## Pull request expectations

- Explain the scenario or governance need behind the change.
- Summarize impacted service patterns, schemas, pipelines, or docs.
- Include evidence that the relevant tests or validation steps were run.
- Call out any approval policy, security-control, or breaking contract changes explicitly.
- Keep changes scoped and avoid unrelated refactors.
