using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Domain.Events;

/// <summary>Base type for domain events raised by aggregates within Azure Mission Workspace.</summary>
public abstract record DomainEvent(Guid AggregateId, DateTimeOffset OccurredAtUtc);

/// <summary>Raised whenever a deployment request transitions between statuses.</summary>
public sealed record DeploymentRequestStatusChanged(
    Guid AggregateId,
    DateTimeOffset OccurredAtUtc,
    DeploymentRequestStatus PreviousStatus,
    DeploymentRequestStatus NewStatus,
    string ActorObjectId)
    : DomainEvent(AggregateId, OccurredAtUtc);
