using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Domain.Entities;

/// <summary>A single normalized resource-level change produced by mapping an ARM what-if result.</summary>
public sealed record DeploymentPlanChange(
    string ResourceId,
    string ResourceType,
    PlanChangeType ChangeType,
    DeploymentRisk Risk,
    IReadOnlyCollection<string> ChangedProperties,
    string? Explanation = null);

/// <summary>
/// The normalized result of template validation and ARM what-if for a deployment request. This
/// model is independent of Azure SDK response classes so that the rest of the system never
/// depends directly on the shape of ARM's what-if payloads.
/// </summary>
public sealed class DeploymentPlan
{
    public DeploymentPlan(
        Guid id,
        Guid deploymentRequestId,
        IReadOnlyCollection<DeploymentPlanChange> changes,
        DeploymentRisk overallRisk,
        DateTimeOffset generatedAtUtc)
    {
        Id = id;
        DeploymentRequestId = deploymentRequestId;
        Changes = changes;
        OverallRisk = overallRisk;
        GeneratedAtUtc = generatedAtUtc;
    }

    public Guid Id { get; }

    public Guid DeploymentRequestId { get; }

    public IReadOnlyCollection<DeploymentPlanChange> Changes { get; }

    public DeploymentRisk OverallRisk { get; }

    public DateTimeOffset GeneratedAtUtc { get; }

    public bool HasDestructiveChanges => Changes.Any(c => c.ChangeType is PlanChangeType.Delete or PlanChangeType.Replace);

    public bool HasUnknownChanges => Changes.Any(c => c.ChangeType == PlanChangeType.Unknown);
}

/// <summary>A single structured finding produced by a policy rule evaluation.</summary>
public sealed record PolicyFinding(
    string RuleId,
    string Title,
    string Description,
    PolicyFindingSeverity Severity,
    string? ResourceId,
    string? PropertyPath,
    string? ActualValue,
    string ExpectedCondition,
    string Remediation,
    string? DocumentationReference);

/// <summary>The aggregate result of evaluating all applicable policy rules against a request and plan.</summary>
public sealed class PolicyEvaluation
{
    public PolicyEvaluation(Guid id, Guid deploymentRequestId, IReadOnlyCollection<PolicyFinding> findings, DateTimeOffset evaluatedAtUtc, DateTimeOffset expiresAtUtc)
    {
        Id = id;
        DeploymentRequestId = deploymentRequestId;
        Findings = findings;
        EvaluatedAtUtc = evaluatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; }

    public Guid DeploymentRequestId { get; }

    public IReadOnlyCollection<PolicyFinding> Findings { get; }

    public DateTimeOffset EvaluatedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public bool HasBlockingFindings => Findings.Any(f => f.Severity == PolicyFindingSeverity.Blocking);

    public bool IsExpired(DateTimeOffset asOfUtc) => asOfUtc >= ExpiresAtUtc;
}

/// <summary>A single required approval for a deployment request, calculated from risk and environment rules.</summary>
public sealed class ApprovalRequirement
{
    public ApprovalRequirement(Guid id, Guid deploymentRequestId, string requiredRole, int requiredApproverCount, bool requiresDistinctFromRequestor)
    {
        Id = id;
        DeploymentRequestId = deploymentRequestId;
        RequiredRole = requiredRole;
        RequiredApproverCount = requiredApproverCount;
        RequiresDistinctFromRequestor = requiresDistinctFromRequestor;
    }

    public Guid Id { get; }

    public Guid DeploymentRequestId { get; }

    public string RequiredRole { get; }

    public int RequiredApproverCount { get; }

    public bool RequiresDistinctFromRequestor { get; }
}

/// <summary>A recorded approval or rejection decision made by an authorized approver.</summary>
public sealed record ApprovalDecision(
    Guid Id,
    Guid DeploymentRequestId,
    Guid ApprovalRequirementId,
    string ApproverObjectId,
    Enums.ApprovalStatus Status,
    string? Comment,
    DateTimeOffset DecidedAtUtc);

/// <summary>Tracks an Azure DevOps pipeline run associated with a deployment request.</summary>
public sealed class PipelineExecution
{
    public PipelineExecution(Guid id, Guid deploymentRequestId, string pipelineName, int? buildId, PipelineStatus status, DateTimeOffset queuedAtUtc)
    {
        Id = id;
        DeploymentRequestId = deploymentRequestId;
        PipelineName = pipelineName;
        BuildId = buildId;
        Status = status;
        QueuedAtUtc = queuedAtUtc;
    }

    public Guid Id { get; }

    public Guid DeploymentRequestId { get; }

    public string PipelineName { get; }

    public int? BuildId { get; private set; }

    public PipelineStatus Status { get; private set; }

    public DateTimeOffset QueuedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void UpdateStatus(PipelineStatus status, DateTimeOffset? completedAtUtc = null)
    {
        Status = status;
        if (completedAtUtc is not null)
        {
            CompletedAtUtc = completedAtUtc;
        }
    }

    public void AssignBuildId(int buildId) => BuildId = buildId;
}

/// <summary>The outcome of an actual Azure deployment executed by the pipeline's declared strategy.</summary>
public sealed record DeploymentExecution(
    Guid Id,
    Guid DeploymentRequestId,
    DeploymentStrategyType Strategy,
    bool Succeeded,
    IReadOnlyDictionary<string, string> Outputs,
    string? ErrorMessage,
    DateTimeOffset CompletedAtUtc);

/// <summary>
/// The immutable record connecting the user request, selected pattern, parameters, source
/// revision, validation results, approvals, pipeline execution, and Azure deployment outcome.
/// References to the individual evidence artifacts are stored as content hashes plus storage
/// locations; secret material is redacted before persistence.
/// </summary>
public sealed class DeploymentEvidence
{
    public DeploymentEvidence(
        Guid id,
        Guid deploymentRequestId,
        IReadOnlyDictionary<string, EvidenceArtifactReference> artifacts,
        DateTimeOffset finalizedAtUtc)
    {
        Id = id;
        DeploymentRequestId = deploymentRequestId;
        Artifacts = artifacts;
        FinalizedAtUtc = finalizedAtUtc;
    }

    public Guid Id { get; }

    public Guid DeploymentRequestId { get; }

    /// <summary>Artifact name (for example "what-if-normalized.json") to its stored reference.</summary>
    public IReadOnlyDictionary<string, EvidenceArtifactReference> Artifacts { get; }

    public DateTimeOffset FinalizedAtUtc { get; }
}

/// <summary>A reference to a single evidence artifact, including an integrity hash of its (redacted) content.</summary>
public sealed record EvidenceArtifactReference(string Name, string Sha256Hash, string StorageUri, DateTimeOffset CreatedAtUtc);
