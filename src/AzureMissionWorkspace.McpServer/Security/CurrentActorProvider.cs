using System.Security.Claims;
using AzureMissionWorkspace.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace AzureMissionWorkspace.McpServer.Security;

/// <summary>Resolves the authenticated human actor for the current request. Never falls back to an unauthenticated identity in non-development environments.</summary>
public interface ICurrentActorProvider
{
    ActorIdentity GetCurrentActor();
}

/// <summary>
/// Resolves the current actor from the authenticated <see cref="ClaimsPrincipal"/>. In local
/// development only, when <c>Authentication:UseFakeActor</c> is enabled and no authenticated user
/// is present, a deterministic fake actor is used so the sample can run without Microsoft Entra ID
/// or any Azure credentials. This fallback is refused outside the Development environment.
/// </summary>
public sealed class HttpContextCurrentActorProvider : ICurrentActorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public HttpContextCurrentActorProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _environment = environment;
    }

    public ActorIdentity GetCurrentActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var objectId = user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var displayName = user.FindFirstValue("name") ?? user.Identity.Name ?? objectId;
            var upn = user.FindFirstValue("preferred_username") ?? user.FindFirstValue(ClaimTypes.Upn) ?? displayName;
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(user.FindAll("roles").Select(c => c.Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new ActorIdentity(objectId, displayName, upn, roles);
        }

        if (_environment.IsDevelopment() && _configuration.GetValue("Authentication:UseFakeActor", false))
        {
            return new ActorIdentity(
                "00000000-0000-0000-0000-0000000000fa",
                "Local Development User",
                "dev-user@contoso.local",
                ["ServicePatternReader", "DeploymentRequestor", "DeploymentApprover", "PlatformEngineer", "PlatformAdministrator", "Auditor"]);
        }

        throw new UnauthorizedAccessException("No authenticated actor is available for this request.");
    }
}
