using Microsoft.AspNetCore.Authorization;

namespace AzureMissionWorkspace.Application.Abstractions.Authorization;

/// <summary>
/// Names of the ASP.NET Core policy-based authorization policies enforced for MCP tool
/// invocations and resource reads. All authorization is enforced server-side; MCP tool
/// descriptions and annotations are documentation only and are never treated as security controls.
/// </summary>
public static class AuthorizationPolicyNames
{
    public const string ServicePatternReader = nameof(ServicePatternReader);
    public const string DeploymentRequestor = nameof(DeploymentRequestor);
    public const string DeploymentApprover = nameof(DeploymentApprover);
    public const string PlatformEngineer = nameof(PlatformEngineer);
    public const string PlatformAdministrator = nameof(PlatformAdministrator);
    public const string Auditor = nameof(Auditor);
}

/// <summary>
/// Resource-based authorization requirement enforcing that an actor may only approve a protected
/// (for example production) deployment request if they are not the original requestor --
/// separation of duties for protected environments.
/// </summary>
public sealed class DistinctApproverRequirement : IAuthorizationRequirement
{
    public static readonly DistinctApproverRequirement Instance = new();
}

/// <summary>Marker requirement for resource-based authorization over a specific deployment request, plan, approval, environment profile, or evidence record.</summary>
public sealed class OwnsOrIsAuthorizedForResourceRequirement : IAuthorizationRequirement
{
    public static readonly OwnsOrIsAuthorizedForResourceRequirement Instance = new();
}
