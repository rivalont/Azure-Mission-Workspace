using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace AzureMissionWorkspace.McpServer.Authorization;

/// <summary>
/// A resource requiring approval-authorization checks: the deployment request being approved and
/// the environment profile it targets. Used with <see cref="DistinctApproverRequirement"/>.
/// </summary>
public sealed record ApprovalAuthorizationResource(DeploymentRequest Request, EnvironmentProfile EnvironmentProfile);

/// <summary>
/// Enforces separation of duties: a requestor may not approve their own deployment request when
/// the target environment is protected (production). This authorization check is enforced
/// server-side and is never bypassed by MCP tool descriptions or client-supplied hints -- the
/// Application layer's <c>RecordApprovalDecisionHandler</c> independently re-enforces the same
/// rule via a domain exception as defense in depth.
/// </summary>
public sealed class DistinctApproverRequirementHandler : AuthorizationHandler<DistinctApproverRequirement, ApprovalAuthorizationResource>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DistinctApproverRequirement requirement, ApprovalAuthorizationResource resource)
    {
        var actorObjectId = context.User.FindFirst("oid")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var isProtectedEnvironment = resource.EnvironmentProfile.EnvironmentType == EnvironmentType.Production
            || resource.EnvironmentProfile.RequiresApprovalForProduction;

        var isSelfApproval = !string.IsNullOrEmpty(actorObjectId)
            && string.Equals(actorObjectId, resource.Request.Requestor.ObjectId, StringComparison.OrdinalIgnoreCase);

        if (!isProtectedEnvironment || !isSelfApproval)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// A resource with an owning requestor, used with <see cref="OwnsOrIsAuthorizedForResourceRequirement"/>
/// to determine whether the current actor may read or mutate a deployment-request-scoped resource.
/// </summary>
public sealed record OwnedResource(string RequestorObjectId);

/// <summary>
/// Grants access to a deployment-request-scoped resource (request, plan, approval, evidence) to its
/// owning requestor, or to any actor holding an elevated platform role.
/// </summary>
public sealed class OwnsOrIsAuthorizedForResourceRequirementHandler : AuthorizationHandler<OwnsOrIsAuthorizedForResourceRequirement, OwnedResource>
{
    private static readonly string[] ElevatedRoles =
    [
        AuthorizationPolicyNames.DeploymentApprover,
        AuthorizationPolicyNames.PlatformEngineer,
        AuthorizationPolicyNames.PlatformAdministrator,
        AuthorizationPolicyNames.Auditor,
    ];

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnsOrIsAuthorizedForResourceRequirement requirement, OwnedResource resource)
    {
        var actorObjectId = context.User.FindFirst("oid")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var isOwner = !string.IsNullOrEmpty(actorObjectId) && string.Equals(actorObjectId, resource.RequestorObjectId, StringComparison.OrdinalIgnoreCase);
        var isElevated = ElevatedRoles.Any(role => context.User.IsInRole(role) || context.User.HasClaim("roles", role));

        if (isOwner || isElevated)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Registers the six authorization policies enforced for MCP tool invocations and resource reads.
/// All authorization is enforced server-side -- MCP tool annotations describe intent only.
/// </summary>
public static class AuthorizationSetup
{
    public static IServiceCollection AddAzureMissionWorkspaceAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicyNames.ServicePatternReader, policy => policy.RequireAssertion(HasAnyKnownRole))
            .AddPolicy(AuthorizationPolicyNames.DeploymentRequestor, policy => policy.RequireRole(AuthorizationPolicyNames.DeploymentRequestor, AuthorizationPolicyNames.PlatformEngineer, AuthorizationPolicyNames.PlatformAdministrator))
            .AddPolicy(AuthorizationPolicyNames.DeploymentApprover, policy => policy.RequireRole(AuthorizationPolicyNames.DeploymentApprover, AuthorizationPolicyNames.PlatformAdministrator))
            .AddPolicy(AuthorizationPolicyNames.PlatformEngineer, policy => policy.RequireRole(AuthorizationPolicyNames.PlatformEngineer, AuthorizationPolicyNames.PlatformAdministrator))
            .AddPolicy(AuthorizationPolicyNames.PlatformAdministrator, policy => policy.RequireRole(AuthorizationPolicyNames.PlatformAdministrator))
            .AddPolicy(AuthorizationPolicyNames.Auditor, policy => policy.RequireRole(AuthorizationPolicyNames.Auditor, AuthorizationPolicyNames.PlatformAdministrator));

        services.AddSingleton<IAuthorizationHandler, DistinctApproverRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, OwnsOrIsAuthorizedForResourceRequirementHandler>();

        return services;
    }

    private static bool HasAnyKnownRole(AuthorizationHandlerContext context)
    {
        string[] knownRoles =
        [
            AuthorizationPolicyNames.ServicePatternReader,
            AuthorizationPolicyNames.DeploymentRequestor,
            AuthorizationPolicyNames.DeploymentApprover,
            AuthorizationPolicyNames.PlatformEngineer,
            AuthorizationPolicyNames.PlatformAdministrator,
            AuthorizationPolicyNames.Auditor,
        ];

        return knownRoles.Any(role => context.User.IsInRole(role) || context.User.HasClaim("roles", role));
    }
}
