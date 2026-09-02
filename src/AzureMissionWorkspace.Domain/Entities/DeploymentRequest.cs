using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.Events;
using AzureMissionWorkspace.Domain.Exceptions;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Domain.Entities;

/// <summary>
/// The structured record describing what a user wants deployed. This is the central aggregate of
/// Azure Mission Workspace: it tracks lifecycle status via <see cref="DeploymentRequestStateMachine"/>,
/// carries the selected service pattern and rendered parameters, and accumulates references to the
/// deployment plan, policy evaluation, approvals, pipeline execution, and evidence produced as the
/// request progresses.
/// </summary>
public sealed class DeploymentRequest
{
    private readonly List<DomainEvent> _domainEvents = [];

    public DeploymentRequest(
        DeploymentRequestId id,
        CorrelationId correlationId,
        ActorIdentity requestor,
        EnvironmentProfileId environmentProfileId,
        string naturalLanguageRequest)
    {
        Id = id;
        CorrelationId = correlationId;
        Requestor = requestor;
        EnvironmentProfileId = environmentProfileId;
        NaturalLanguageRequest = naturalLanguageRequest;
        Status = DeploymentRequestStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public DeploymentRequestId Id { get; }

    public CorrelationId CorrelationId { get; }

    public ActorIdentity Requestor { get; }

    public EnvironmentProfileId EnvironmentProfileId { get; }

    public string NaturalLanguageRequest { get; }

    public ServicePatternId? SelectedServicePatternId { get; private set; }

    public ServicePatternVersion? SelectedServicePatternVersion { get; private set; }

    public DeploymentParameters? Parameters { get; private set; }

    public DeploymentRequestStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token. Incremented on every state mutation.</summary>
    public int Version { get; private set; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Applies a status transition, enforcing legality via <see cref="DeploymentRequestStateMachine"/>
    /// and optimistic concurrency via <paramref name="expectedVersion"/>.
    /// </summary>
    public void TransitionTo(DeploymentRequestStatus newStatus, ActorIdentity actor, int expectedVersion)
    {
        EnsureExpectedVersion(expectedVersion);

        if (!DeploymentRequestStateMachine.CanTransition(Status, newStatus))
        {
            throw new IllegalDeploymentRequestTransitionException(Status, newStatus);
        }

        var previous = Status;
        Status = newStatus;
        Touch();

        _domainEvents.Add(new DeploymentRequestStatusChanged(Id.Value, DateTimeOffset.UtcNow, previous, newStatus, actor.ObjectId));
    }

    public void SelectServicePattern(ServicePatternId patternId, ServicePatternVersion version, ActorIdentity actor, int expectedVersion)
    {
        EnsureExpectedVersion(expectedVersion);
        SelectedServicePatternId = patternId;
        SelectedServicePatternVersion = version;
        TransitionTo(DeploymentRequestStatus.PatternSelected, actor, expectedVersion);
    }

    public void RenderParameters(DeploymentParameters parameters, ActorIdentity actor, int expectedVersion)
    {
        EnsureExpectedVersion(expectedVersion);
        Parameters = parameters;
        TransitionTo(DeploymentRequestStatus.ParametersRendered, actor, expectedVersion);
    }

    private void EnsureExpectedVersion(int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConcurrencyConflictException(nameof(DeploymentRequest), Id.ToString());
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        Version++;
    }
}
