using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Domain.Exceptions;

/// <summary>Base type for all domain-specific exceptions raised by Azure Mission Workspace.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when a caller attempts to move a <see cref="Entities.DeploymentRequest"/> to a status
/// that is not a legal transition from its current status.
/// </summary>
public sealed class IllegalDeploymentRequestTransitionException : DomainException
{
    public IllegalDeploymentRequestTransitionException(DeploymentRequestStatus from, DeploymentRequestStatus to)
        : base($"Cannot transition deployment request from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }

    public DeploymentRequestStatus From { get; }

    public DeploymentRequestStatus To { get; }
}

/// <summary>
/// Thrown when a caller attempts to mutate an entity using a stale concurrency token
/// (optimistic concurrency conflict).
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string entityName, string entityId)
        : base($"Concurrency conflict detected for {entityName} '{entityId}'. The entity was modified by another operation.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public string EntityName { get; }

    public string EntityId { get; }
}

/// <summary>
/// Thrown when separation-of-duties rules are violated, such as a requestor attempting to
/// approve their own protected-environment deployment.
/// </summary>
public sealed class SeparationOfDutiesViolationException : DomainException
{
    public SeparationOfDutiesViolationException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when a deployment parameter set fails required validation rules.</summary>
public sealed class InvalidDeploymentParametersException : DomainException
{
    public InvalidDeploymentParametersException(string message) : base(message)
    {
    }
}
