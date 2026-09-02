namespace AzureMissionWorkspace.Application.Abstractions.AzureDevOps;

/// <summary>Reference to a deployment branch created or reused for a deployment request.</summary>
public sealed record DeploymentBranch(string Name, string BaseBranch, string LatestCommitSha);

/// <summary>Manages the deployment-request repository model: branches and generated (non-secret) artifacts.</summary>
public interface IRepositoryService
{
    Task<DeploymentBranch> CreateOrReuseDeploymentBranchAsync(string deploymentRequestId, CancellationToken cancellationToken = default);

    /// <summary>Commits generated, non-secret parameter artifacts (for example a rendered .bicepparam file) to the deployment branch.</summary>
    Task<string> CommitArtifactsAsync(DeploymentBranch branch, IReadOnlyDictionary<string, string> filePathToContent, string commitMessage, CancellationToken cancellationToken = default);
}

/// <summary>Metadata included in a generated pull request describing a deployment request.</summary>
public sealed record PullRequestSummary(
    string DeploymentRequestId,
    string RequestorDisplayName,
    string ServicePatternId,
    string ServicePatternVersion,
    string EnvironmentProfileId,
    string TargetScope,
    string RiskClassification,
    string PolicySummary,
    string WhatIfArtifactReference,
    string ApprovalRequirementsSummary);

/// <summary>Creates and manages pull requests carrying deployment-request metadata. Never places secrets in Git.</summary>
public interface IPullRequestService
{
    Task<int> CreatePullRequestAsync(DeploymentBranch branch, PullRequestSummary summary, CancellationToken cancellationToken = default);
}

/// <summary>Status of a queued pipeline run, expressed independently of the Azure DevOps REST API shape.</summary>
public sealed record PipelineRunStatus(int BuildId, string StatusName, string? Result);

/// <summary>Queues and monitors Azure DevOps validation and deployment pipelines.</summary>
public interface IPipelineService
{
    Task<int> QueueValidationPipelineAsync(string deploymentRequestId, string correlationId, CancellationToken cancellationToken = default);

    Task<int> QueueDeploymentPipelineAsync(string deploymentRequestId, string correlationId, CancellationToken cancellationToken = default);

    Task<PipelineRunStatus> GetStatusAsync(int buildId, CancellationToken cancellationToken = default);
}

/// <summary>Coordinates environment-based or externally configured approvals for protected deployments.</summary>
public interface IApprovalService
{
    Task<bool> IsApprovalSatisfiedAsync(string deploymentRequestId, CancellationToken cancellationToken = default);
}

/// <summary>Retrieves published pipeline artifacts such as compiled templates and what-if results.</summary>
public interface IArtifactService
{
    Task<Stream> DownloadArtifactAsync(int buildId, string artifactName, CancellationToken cancellationToken = default);

    Task PublishArtifactAsync(int buildId, string artifactName, Stream content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Top-level abstraction over the Azure DevOps REST API surface used by Azure Mission Workspace.
/// Composes the finer-grained services below and is the single seam infrastructure adapters
/// implement to integrate with a real Azure DevOps organization.
/// </summary>
public interface IAzureDevOpsClient
{
    IRepositoryService Repositories { get; }

    IPullRequestService PullRequests { get; }

    IPipelineService Pipelines { get; }

    IApprovalService Approvals { get; }

    IArtifactService Artifacts { get; }
}
