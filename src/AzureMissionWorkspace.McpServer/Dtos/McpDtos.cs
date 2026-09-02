using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.McpServer.Dtos;

/// <summary>Flat, JSON-serialization-friendly projection of a <see cref="DeploymentRequest"/> for MCP tool responses.</summary>
public sealed record DeploymentRequestDto(
    Guid Id,
    string CorrelationId,
    string RequestorObjectId,
    string RequestorDisplayName,
    string EnvironmentProfileId,
    string NaturalLanguageRequest,
    string? ServicePatternId,
    string? ServicePatternVersion,
    string Status,
    int Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyDictionary<string, string>? RedactedParameters)
{
    public static DeploymentRequestDto FromDomain(DeploymentRequest request) => new(
        request.Id.Value,
        request.CorrelationId.ToString(),
        request.Requestor.ObjectId,
        request.Requestor.DisplayName,
        request.EnvironmentProfileId.Value,
        request.NaturalLanguageRequest,
        request.SelectedServicePatternId?.Value,
        request.SelectedServicePatternVersion?.Value,
        request.Status.ToString(),
        request.Version,
        request.CreatedAtUtc,
        request.UpdatedAtUtc,
        request.Parameters?.ToRedactedDictionary());
}

/// <summary>Flat projection of an approved <see cref="ServicePattern"/> for MCP tool responses.</summary>
public sealed record ServicePatternInputDto(string Name, string Type, string Description, bool IsRequired, bool IsSecret, string? DefaultValue);

public sealed record ServicePatternDto(
    string Id,
    string Version,
    string DisplayName,
    string Description,
    string DeploymentStrategy,
    string Scope,
    IReadOnlyCollection<string> SupportedClouds,
    IReadOnlyCollection<string> SupportedEnvironmentTypes,
    IReadOnlyCollection<string> SupportedRegions,
    IReadOnlyCollection<ServicePatternInputDto> RequiredInputs,
    IReadOnlyCollection<ServicePatternInputDto> OptionalInputs,
    bool IsDeprecated)
{
    public static ServicePatternDto FromDomain(ServicePattern pattern) => new(
        pattern.Id.Value,
        pattern.Version.Value,
        pattern.DisplayName,
        pattern.Description,
        pattern.DeploymentStrategy.ToString(),
        pattern.Scope.ToString(),
        pattern.SupportedClouds.Select(c => c.ToString()).ToArray(),
        pattern.SupportedEnvironmentTypes.Select(e => e.ToString()).ToArray(),
        pattern.SupportedRegions,
        pattern.RequiredInputs.Select(ToDto).ToArray(),
        pattern.OptionalInputs.Select(ToDto).ToArray(),
        pattern.IsDeprecated);

    private static ServicePatternInputDto ToDto(ServicePatternInput input) => new(input.Name, input.Type, input.Description, input.IsRequired, input.IsSecret, input.DefaultValue);
}

/// <summary>Flat projection of a <see cref="DeploymentPlanChange"/>.</summary>
public sealed record DeploymentPlanChangeDto(string ResourceId, string ResourceType, string ChangeType, string Risk, IReadOnlyCollection<string> ChangedProperties, string? Explanation);

/// <summary>Flat projection of a <see cref="DeploymentPlan"/>. This is the deterministic, authoritative result -- any accompanying explanation is generated separately and must never override it.</summary>
public sealed record DeploymentPlanDto(
    Guid Id,
    Guid DeploymentRequestId,
    IReadOnlyCollection<DeploymentPlanChangeDto> Changes,
    string OverallRisk,
    bool HasDestructiveChanges,
    bool HasUnknownChanges,
    DateTimeOffset GeneratedAtUtc)
{
    public static DeploymentPlanDto FromDomain(DeploymentPlan plan) => new(
        plan.Id,
        plan.DeploymentRequestId,
        plan.Changes.Select(c => new DeploymentPlanChangeDto(c.ResourceId, c.ResourceType, c.ChangeType.ToString(), c.Risk.ToString(), c.ChangedProperties, c.Explanation)).ToArray(),
        plan.OverallRisk.ToString(),
        plan.HasDestructiveChanges,
        plan.HasUnknownChanges,
        plan.GeneratedAtUtc);
}

/// <summary>Flat projection of a single <see cref="PolicyFinding"/>.</summary>
public sealed record PolicyFindingDto(
    string RuleId,
    string Title,
    string Description,
    string Severity,
    string? ResourceId,
    string? PropertyPath,
    string? ActualValue,
    string ExpectedCondition,
    string Remediation,
    string? DocumentationReference)
{
    public static PolicyFindingDto FromDomain(PolicyFinding finding) => new(
        finding.RuleId, finding.Title, finding.Description, finding.Severity.ToString(), finding.ResourceId,
        finding.PropertyPath, finding.ActualValue, finding.ExpectedCondition, finding.Remediation, finding.DocumentationReference);
}

/// <summary>Flat projection of a <see cref="PolicyEvaluation"/>.</summary>
public sealed record PolicyEvaluationDto(
    Guid Id,
    Guid DeploymentRequestId,
    IReadOnlyCollection<PolicyFindingDto> Findings,
    bool HasBlockingFindings,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public static PolicyEvaluationDto FromDomain(PolicyEvaluation evaluation) => new(
        evaluation.Id,
        evaluation.DeploymentRequestId,
        evaluation.Findings.Select(PolicyFindingDto.FromDomain).ToArray(),
        evaluation.HasBlockingFindings,
        evaluation.EvaluatedAtUtc,
        evaluation.ExpiresAtUtc);
}

/// <summary>Flat projection of a required approval.</summary>
public sealed record ApprovalRequirementDto(Guid Id, Guid DeploymentRequestId, string RequiredRole, int RequiredApproverCount, bool RequiresDistinctFromRequestor)
{
    public static ApprovalRequirementDto FromDomain(ApprovalRequirement requirement) => new(
        requirement.Id, requirement.DeploymentRequestId, requirement.RequiredRole, requirement.RequiredApproverCount, requirement.RequiresDistinctFromRequestor);
}

/// <summary>Flat projection of a queued pipeline execution.</summary>
public sealed record PipelineExecutionDto(Guid Id, Guid DeploymentRequestId, string PipelineName, int? BuildId, string Status, DateTimeOffset QueuedAtUtc)
{
    public static PipelineExecutionDto FromDomain(PipelineExecution execution) => new(
        execution.Id, execution.DeploymentRequestId, execution.PipelineName, execution.BuildId, execution.Status.ToString(), execution.QueuedAtUtc);
}

/// <summary>Flat projection of a finalized deployment-evidence package. Secret values are never present -- artifacts are referenced by hash and storage location only.</summary>
public sealed record EvidenceArtifactDto(string Name, string Sha256Hash, string StorageUri, DateTimeOffset CreatedAtUtc);

public sealed record DeploymentEvidenceDto(Guid Id, Guid DeploymentRequestId, IReadOnlyCollection<EvidenceArtifactDto> Artifacts, DateTimeOffset FinalizedAtUtc)
{
    public static DeploymentEvidenceDto FromDomain(DeploymentEvidence evidence) => new(
        evidence.Id,
        evidence.DeploymentRequestId,
        evidence.Artifacts.Values.Select(a => new EvidenceArtifactDto(a.Name, a.Sha256Hash, a.StorageUri, a.CreatedAtUtc)).ToArray(),
        evidence.FinalizedAtUtc);
}
