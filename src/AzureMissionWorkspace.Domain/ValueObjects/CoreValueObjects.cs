namespace AzureMissionWorkspace.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a <see cref="Entities.DeploymentRequest"/>.</summary>
public readonly record struct DeploymentRequestId(Guid Value)
{
    public static DeploymentRequestId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>Strongly typed identifier for a <see cref="Entities.ServicePattern"/>.</summary>
public readonly record struct ServicePatternId(string Value)
{
    public override string ToString() => Value;
}

/// <summary>Semantic version identifying a specific revision of a service pattern.</summary>
public readonly record struct ServicePatternVersion(string Value)
{
    public override string ToString() => Value;
}

/// <summary>Strongly typed identifier for an <see cref="Entities.EnvironmentProfile"/>.</summary>
public readonly record struct EnvironmentProfileId(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Correlation identifier propagated across MCP tool calls, application commands, telemetry,
/// pipeline requests, and evidence records for end-to-end traceability.
/// </summary>
public readonly record struct CorrelationId(Guid Value)
{
    public static CorrelationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>
/// The authenticated human (or, rarely, service) identity that initiated an operation. This
/// identity must be preserved in every command, audit record, pipeline request, and evidence
/// package -- the Azure DevOps service connection or managed identity used to execute a
/// deployment never replaces the human actor in the audit trail.
/// </summary>
public sealed record ActorIdentity(string ObjectId, string DisplayName, string UserPrincipalName, IReadOnlyCollection<string> Roles)
{
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

/// <summary>The exact source revision (commit) that produced a rendered parameter set and evidence package.</summary>
public sealed record SourceRevision(string RepositoryUrl, string Branch, string CommitSha);

/// <summary>
/// The set of parameter values collected for a deployment request, keyed by service-pattern input
/// name. Secret values are represented as references (for example, Key Vault secret identifiers or
/// pipeline secret variable names) and are never stored as literal secret material.
/// </summary>
public sealed class DeploymentParameters
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _secretInputNames;

    public DeploymentParameters(IReadOnlyCollection<string> secretInputNames)
    {
        _secretInputNames = new HashSet<string>(secretInputNames, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    public void Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _values[name] = value;
    }

    public bool TryGetValue(string name, out string? value) => _values.TryGetValue(name, out value);

    public bool IsSecret(string name) => _secretInputNames.Contains(name);

    /// <summary>
    /// Returns a copy of the parameter values with secret-input values redacted. Safe to use for
    /// logs, evidence, chat responses, and pull-request descriptions.
    /// </summary>
    public IReadOnlyDictionary<string, string> ToRedactedDictionary()
    {
        return _values.ToDictionary(
            kvp => kvp.Key,
            kvp => IsSecret(kvp.Key) ? "***redacted***" : kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}
