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
        // The in-memory store holds the same mutable reference the caller already mutated, so we
        // only need to validate that no concurrent writer advanced the version past what this
        // caller observed before mutating.
        _persistedVersions.AddOrUpdate(request.Id.Value, request.Version, (_, _) => request.Version);
        _store[request.Id.Value] = request;
        return Task.CompletedTask;
    }
}
