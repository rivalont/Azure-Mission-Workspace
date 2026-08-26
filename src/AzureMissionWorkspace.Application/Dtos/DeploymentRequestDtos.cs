using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Application.Dtos;

/// <summary>Input to create a new deployment request draft.</summary>
public sealed record CreateDeploymentRequestInput(string RequestorObjectId, string RequestorDisplayName, string RequestorUpn, string EnvironmentProfileId, string NaturalLanguageRequest);

/// <summary>Input to select a service pattern for an existing deployment request.</summary>
public sealed record SelectServicePatternInput(Guid DeploymentRequestId, string ServicePatternId, string ServicePatternVersion, int ExpectedVersion);

/// <summary>Input carrying candidate parameter values supplied by the requestor for the selected pattern.</summary>
public sealed record UpdateDeploymentRequestInput(Guid DeploymentRequestId, IReadOnlyDictionary<string, string> ParameterValues, int ExpectedVersion);

/// <summary>Input to record a human approval or rejection decision.</summary>
public sealed record RecordApprovalDecisionInput(Guid DeploymentRequestId, Guid ApprovalRequirementId, string ApproverObjectId, ApprovalStatus Decision, string? Comment);

/// <summary>Summary view of a deployment request returned by read operations.</summary>
public sealed record DeploymentRequestSummary(
    Guid Id,
    DeploymentRequestStatus Status,
    string RequestorDisplayName,
    string EnvironmentProfileId,
    string? ServicePatternId,
    string? ServicePatternVersion,
    int Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
