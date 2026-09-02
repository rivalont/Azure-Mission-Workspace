using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureMissionWorkspace.McpServer.Security;

/// <summary>
/// Local-development-only authentication handler that authenticates every request as the
/// deterministic fake actor used by <see cref="HttpContextCurrentActorProvider"/>, so the starter
/// sample can be exercised end to end without Microsoft Entra ID or any Azure credentials. This
/// handler is only ever registered when <c>Authentication:UseFakeActor</c> is enabled and the host
/// environment is Development (see <c>Program.cs</c>); it must never be enabled outside local
/// development.
/// </summary>
public sealed class FakeDevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevelopmentFakeActor";

    public FakeDevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("oid", "00000000-0000-0000-0000-0000000000fa"),
            new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-0000000000fa"),
            new Claim("name", "Local Development User"),
            new Claim(ClaimTypes.Name, "Local Development User"),
            new Claim("preferred_username", "dev-user@contoso.local"),
            new Claim(ClaimTypes.Role, "ServicePatternReader"),
            new Claim(ClaimTypes.Role, "DeploymentRequestor"),
            new Claim(ClaimTypes.Role, "DeploymentApprover"),
            new Claim(ClaimTypes.Role, "PlatformEngineer"),
            new Claim(ClaimTypes.Role, "PlatformAdministrator"),
            new Claim(ClaimTypes.Role, "Auditor"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
