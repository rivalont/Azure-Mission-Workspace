using System.Collections.Concurrent;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Infrastructure.Persistence;

/// <summary>In-memory <see cref="IDeploymentEvidenceRepository"/> for local development and tests.</summary>
public sealed class InMemoryDeploymentEvidenceRepository : IDeploymentEvidenceRepository
{
    private readonly ConcurrentDictionary<Guid, DeploymentEvidence> _store = new();

    public Task<DeploymentEvidence?> FindByDeploymentRequestIdAsync(DeploymentRequestId deploymentRequestId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(deploymentRequestId.Value, out var evidence);
        return Task.FromResult(evidence);
    }

    public Task SaveAsync(DeploymentEvidence evidence, CancellationToken cancellationToken = default)
    {
        _store[evidence.DeploymentRequestId] = evidence;
        return Task.CompletedTask;
    }
}
