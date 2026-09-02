using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Application.Abstractions.Bicep;

/// <summary>Result of compiling a Bicep entry-point file to an ARM JSON template.</summary>
public sealed record BicepCompilationResult(bool Succeeded, string? CompiledTemplateJson, IReadOnlyCollection<string> Diagnostics);

/// <summary>Compiles approved Bicep entry-point files into ARM JSON templates. Never accepts arbitrary, dynamically concatenated Bicep source.</summary>
public interface IBicepCompiler
{
    Task<BicepCompilationResult> CompileAsync(string bicepFilePath, CancellationToken cancellationToken = default);
}

/// <summary>Renders a deterministic <c>.bicepparam</c> artifact from validated deployment parameters. Never writes secret values.</summary>
public interface IBicepParameterRenderer
{
    Task<string> RenderAsync(ServicePattern pattern, DeploymentRequest request, EnvironmentProfile environmentProfile, CancellationToken cancellationToken = default);
}

/// <summary>Resolves declared, pinned module references for a service pattern. Never accepts arbitrary module references from requestors.</summary>
public interface IBicepModuleResolver
{
    Task<IReadOnlyCollection<string>> ResolveModuleReferencesAsync(ServicePattern pattern, CancellationToken cancellationToken = default);
}

/// <summary>Abstraction over a private Bicep module registry (for example an Azure Container Registry-backed registry).</summary>
public interface IBicepRegistryClient
{
    Task<bool> IsModuleAvailableAsync(string moduleReference, CancellationToken cancellationToken = default);

    Task RestoreAsync(IReadOnlyCollection<string> moduleReferences, CancellationToken cancellationToken = default);
}

/// <summary>Result of linting a Bicep file against the organization's bicepconfig.json.</summary>
public sealed record BicepLintResult(bool Succeeded, IReadOnlyCollection<string> Diagnostics);

/// <summary>Runs Bicep linting using the organization's bicepconfig.json.</summary>
public interface IBicepLinter
{
    Task<BicepLintResult> LintAsync(string bicepFilePath, CancellationToken cancellationToken = default);
}

/// <summary>Result of validating a compiled ARM template at its declared scope.</summary>
public sealed record TemplateValidationResult(bool IsValid, IReadOnlyCollection<string> Errors);

/// <summary>Validates a compiled ARM template against Azure Resource Manager at the service pattern's declared scope.</summary>
public interface ITemplateValidationService
{
    Task<TemplateValidationResult> ValidateAsync(string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default);
}

/// <summary>Raw, unnormalized what-if result as returned by Azure Resource Manager (or a fake implementation).</summary>
public sealed record RawWhatIfResult(string RawJson);

/// <summary>Executes an Azure Resource Manager what-if operation and returns the raw result for normalization.</summary>
public interface IWhatIfService
{
    Task<RawWhatIfResult> ExecuteWhatIfAsync(string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default);
}

/// <summary>
/// Maps a raw, Azure SDK-shaped what-if result into the normalized, SDK-independent
/// <see cref="DeploymentPlan"/> model, applying deterministic risk calculation via
/// <see cref="Domain.Services.DeploymentRiskCalculator"/>.
/// </summary>
public interface IWhatIfResultNormalizer
{
    DeploymentPlan Normalize(Guid deploymentRequestId, RawWhatIfResult rawResult, EnvironmentType environmentType);
}

/// <summary>Executes an approved deployment using the service pattern's declared deployment strategy.</summary>
public interface IDeploymentService
{
    Task<DeploymentExecution> DeployAsync(DeploymentRequest request, ServicePattern pattern, CancellationToken cancellationToken = default);
}

/// <summary>A single deployment execution strategy (for example standard ARM template deployment or Deployment Stacks).</summary>
public interface IDeploymentStrategy
{
    DeploymentStrategyType StrategyType { get; }

    Task<DeploymentExecution> ExecuteAsync(DeploymentRequest request, ServicePattern pattern, CancellationToken cancellationToken = default);
}
