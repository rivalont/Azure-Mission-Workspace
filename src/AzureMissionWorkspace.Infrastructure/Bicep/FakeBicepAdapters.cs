using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.ServicePatterns.Loading;

namespace AzureMissionWorkspace.Infrastructure.Bicep;

/// <summary>
/// Deterministic fake <see cref="IBicepCompiler"/>. Reads the entry-point Bicep file from disk and
/// produces a minimal, valid-looking ARM JSON template shell so downstream validation/what-if
/// stages have compiled-template content to work with, without requiring the real Bicep CLI.
/// </summary>
public sealed class FakeBicepCompiler : IBicepCompiler
{
    public Task<BicepCompilationResult> CompileAsync(string bicepFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(bicepFilePath))
        {
            return Task.FromResult(new BicepCompilationResult(false, null, [$"Bicep entry-point file not found: {bicepFilePath}"]));
        }

        var template = $$"""
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "metadata": { "_generator": { "name": "azure-mission-workspace-fake-compiler", "source": "{{Path.GetFileName(bicepFilePath)}}" } },
              "resources": []
            }
            """;

        return Task.FromResult(new BicepCompilationResult(true, template, []));
    }
}

/// <summary>Deterministic fake <see cref="IBicepLinter"/>. Always succeeds for the starter solution; real linting is delegated to the Bicep CLI in the validation pipeline.</summary>
public sealed class FakeBicepLinter : IBicepLinter
{
    public Task<BicepLintResult> LintAsync(string bicepFilePath, CancellationToken cancellationToken = default)
        => Task.FromResult(new BicepLintResult(true, []));
}

/// <summary>Deterministic fake <see cref="ITemplateValidationService"/>. Always succeeds for the starter solution; real validation happens against Azure Resource Manager in the pipeline.</summary>
public sealed class FakeTemplateValidationService : ITemplateValidationService
{
    public Task<TemplateValidationResult> ValidateAsync(string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default)
        => Task.FromResult(new TemplateValidationResult(true, []));
}

/// <summary>Resolves the pinned module references declared by a service pattern descriptor. Never accepts arbitrary module references from requestors.</summary>
public sealed class DescriptorBackedBicepModuleResolver : IBicepModuleResolver
{
    private readonly FileSystemServicePatternRepository _repository;

    public DescriptorBackedBicepModuleResolver(FileSystemServicePatternRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<string>> ResolveModuleReferencesAsync(ServicePattern pattern, CancellationToken cancellationToken = default)
    {
        var descriptor = _repository.FindDescriptor(pattern.Id, pattern.Version);

        IReadOnlyCollection<string> references = descriptor?.ModuleReferences
            .Select(m => $"{m.Reference}:{m.Version}")
            .ToArray()
            ?? [];

        return Task.FromResult(references);
    }
}

/// <summary>Deterministic fake <see cref="IBicepRegistryClient"/> that treats every pinned module reference as available.</summary>
public sealed class FakeBicepRegistryClient : IBicepRegistryClient
{
    public Task<bool> IsModuleAvailableAsync(string moduleReference, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task RestoreAsync(IReadOnlyCollection<string> moduleReferences, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
