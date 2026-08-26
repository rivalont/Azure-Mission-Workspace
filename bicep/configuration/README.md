# Bicep configuration

This folder contains repository-level Bicep analyzer settings used by local validation and Azure DevOps pipelines.

## Intent

- Promote secure defaults such as protected secret parameters and avoidance of hard-coded cloud endpoints.
- Keep modules maintainable by surfacing unused parameters, conflicting metadata, and excessive outputs.
- Encourage portability across Azure clouds by warning on hard-coded locations.

The private registry alias documents how pinned modules are expected to be resolved outside this starter repository.
