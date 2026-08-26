using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.Abstractions.Services;

/// <summary>A structured capability extracted from a natural-language deployment request.</summary>
public sealed record ExtractedCapability(string Name, IReadOnlyDictionary<string, string> Constraints);

/// <summary>The structured result produced by intent extraction, prior to pattern recommendation.</summary>
public sealed record IntentExtractionResult(IReadOnlyCollection<ExtractedCapability> Capabilities, string? Notes);

/// <summary>
/// Converts a natural-language capability request into structured capabilities and constraints.
/// This is intentionally an optional abstraction: the starter solution ships only a deterministic
/// fake implementation so that no external AI service is required to compile or run it. A real
/// implementation (for example backed by an LLM) may be substituted, but it never bypasses
/// downstream validation, policy, or approval controls -- it only proposes structure for a human
/// to confirm.
/// </summary>
public interface IIntentExtractionService
{
    Task<IntentExtractionResult> ExtractAsync(string naturalLanguageRequest, CancellationToken cancellationToken = default);
}

/// <summary>A single scored candidate produced by pattern recommendation.</summary>
public sealed record ServicePatternRecommendation(ServicePatternId PatternId, ServicePatternVersion Version, double Score, string Rationale);

/// <summary>
/// Scores the approved service-pattern catalog against extracted capabilities. The recommendation
/// is advisory only: the requestor must explicitly select or accept a pattern before any further
/// processing occurs.
/// </summary>
public interface IPatternRecommendationService
{
    Task<IReadOnlyCollection<ServicePatternRecommendation>> RecommendAsync(IntentExtractionResult intent, EnvironmentType environmentType, AzureCloud cloud, CancellationToken cancellationToken = default);
}

/// <summary>Result of checking whether a specific Azure capability is known to be available in a target cloud/region.</summary>
public enum ServiceAvailability
{
    Available,
    Unavailable,
    Indeterminate,
}

/// <summary>
/// Reports whether a given Azure resource type, SKU, API version, Azure Policy definition, or
/// Azure Verified Module is available for a target cloud and region. Implementations must return
/// <see cref="ServiceAvailability.Indeterminate"/> rather than guessing when compatibility cannot
/// be verified -- callers must treat an indeterminate result as "requires manual confirmation",
/// never as an implicit approval.
/// </summary>
public interface IServiceAvailabilityProvider
{
    Task<ServiceAvailability> CheckAvailabilityAsync(AzureCloud cloud, string region, string resourceTypeOrFeature, CancellationToken cancellationToken = default);
}

/// <summary>
/// Summarizes a deterministic <see cref="DeploymentPlan"/> or <see cref="PolicyEvaluation"/> in
/// natural language for a human user. Explanations are clearly derived from, and must never
/// override or replace, the underlying deterministic findings.
/// </summary>
public interface IExplanationService
{
    Task<string> ExplainDeploymentPlanAsync(DeploymentPlan plan, CancellationToken cancellationToken = default);

    Task<string> ExplainPolicyFindingAsync(PolicyFinding finding, CancellationToken cancellationToken = default);
}
