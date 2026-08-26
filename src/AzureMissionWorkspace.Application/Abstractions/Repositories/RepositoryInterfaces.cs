using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Application.Abstractions.Repositories;

/// <summary>
/// Persistence contract for <see cref="DeploymentRequest"/> aggregates. Implementations must
/// enforce optimistic concurrency using the aggregate's <see cref="DeploymentRequest.Version"/>.
/// The starter solution ships an in-memory implementation; the interface is designed so that
/// Azure Cosmos DB, Azure SQL, or another approved store can be substituted without changing the
/// domain or application layers.
/// </summary>
public interface IDeploymentRequestRepository
{
    Task<DeploymentRequest?> FindByIdAsync(DeploymentRequestId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeploymentRequest>> FindByRequestorAsync(string requestorObjectId, CancellationToken cancellationToken = default);

    Task AddAsync(DeploymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Persists mutations to an existing request, throwing on optimistic concurrency conflicts.</summary>
    Task SaveAsync(DeploymentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Read-only catalog access for approved <see cref="ServicePattern"/> definitions.</summary>
public interface IServicePatternRepository
{
    Task<ServicePattern?> FindAsync(ServicePatternId id, ServicePatternVersion version, CancellationToken cancellationToken = default);

    Task<ServicePattern?> FindLatestAsync(ServicePatternId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ServicePattern>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>Read-only access to platform-administrator-controlled <see cref="EnvironmentProfile"/> records.</summary>
public interface IEnvironmentProfileRepository
{
    Task<EnvironmentProfile?> FindByIdAsync(EnvironmentProfileId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EnvironmentProfile>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>Persistence contract for approval requirements and recorded decisions.</summary>
public interface IApprovalRepository
{
    Task<IReadOnlyCollection<ApprovalRequirement>> GetRequirementsAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default);

    Task AddRequirementAsync(ApprovalRequirement requirement, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ApprovalDecision>> GetDecisionsAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default);

    Task RecordDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default);
}

/// <summary>Persistence contract for finalized deployment evidence packages.</summary>
public interface IDeploymentEvidenceRepository
{
    Task<DeploymentEvidence?> FindByDeploymentRequestIdAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default);

    Task SaveAsync(DeploymentEvidence evidence, CancellationToken cancellationToken = default);
}
