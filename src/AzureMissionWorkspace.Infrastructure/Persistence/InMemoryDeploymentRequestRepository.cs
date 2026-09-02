using System.Collections.Concurrent;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Exceptions;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Infrastructure.Persistence;

/// <summary>
/// In-memory <see cref="IDeploymentRequestRepository"/> for local development and tests. Enforces
/// optimistic concurrency by comparing the caller's expectation of the stored version at save
/// time -- the same check the domain aggregate itself performs on every mutation.
/// </summary>
public sealed class InMemoryDeploymentRequestRepository : IDeploymentRequestRepository
{
    private readonly ConcurrentDictionary<Guid, DeploymentRequest> _store = new();
    private readonly ConcurrentDictionary<Guid, int> _persistedVersions = new();

    public Task<DeploymentRequest?> FindByIdAsync(DeploymentRequestId id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id.Value, out var request);
        return Task.FromResult(request);
    }

    public Task<IReadOnlyCollection<DeploymentRequest>> FindByRequestorAsync(string requestorObjectId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<DeploymentRequest> matches = _store.Values
            .Where(r => string.Equals(r.Requestor.ObjectId, requestorObjectId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(matches);
    }

    /// <summary>
    /// Lists every stored deployment request currently in one of the given statuses. This is a
    /// concrete-type-only convenience used by the Worker for reconciliation and approval-expiration
    /// sweeps -- it is intentionally not part of <see cref="IDeploymentRequestRepository"/> so that
    /// Application-layer test doubles are unaffected by it.
    /// </summary>
    public IReadOnlyCollection<DeploymentRequest> ListByStatus(params Domain.Enums.DeploymentRequestStatus[] statuses)
        => _store.Values.Where(r => statuses.Contains(r.Status)).ToArray();

    public Task AddAsync(DeploymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_store.TryAdd(request.Id.Value, request))
        {
            throw new ConcurrencyConflictException(nameof(DeploymentRequest), request.Id.ToString());
        }

        _persistedVersions[request.Id.Value] = request.Version;
        return Task.CompletedTask;
    }

    public Task SaveAsync(DeploymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_persistedVersions.TryGetValue(request.Id.Value, out var persistedVersion))
        {
            throw new ConcurrencyConflictException(nameof(DeploymentRequest), request.Id.ToString());
        }

        // DeploymentRequest increments Version on mutation; persisted version should be exactly one behind.
        if (persistedVersion != request.Version - 1)
        {
            throw new ConcurrencyConflictException(nameof(DeploymentRequest), request.Id.ToString());
        }

        _persistedVersions[request.Id.Value] = request.Version;
        _store[request.Id.Value] = request;
        return Task.CompletedTask;
    }
}
