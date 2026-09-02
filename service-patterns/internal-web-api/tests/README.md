# Internal Web API pattern tests

Pattern-level validation should confirm:

- `service-pattern.yaml` conforms to `schemas/service-pattern.schema.json`
- `input-schema.json` accepts valid requests and rejects undeclared properties
- `main.bicep` builds successfully and exposes the documented outputs
- Example parameters remain aligned with required and optional inputs

The authoritative automated execution lives in the .NET `ServicePatterns.Tests` project and CI/CD pipelines. This folder documents expected coverage and provides a representative request payload.
