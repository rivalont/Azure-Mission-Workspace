using AzureMissionWorkspace.Application.Abstractions.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzureMissionWorkspace.McpServer.Health;

/// <summary>
/// Readiness check that verifies the approved service-pattern catalog has loaded successfully from
/// the local file system before this instance is considered ready to serve MCP tool calls.
/// </summary>
public sealed class ServicePatternCatalogHealthCheck : IHealthCheck
{
    private readonly IServicePatternRepository _patterns;

    public ServicePatternCatalogHealthCheck(IServicePatternRepository patterns)
    {
        _patterns = patterns;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var patterns = await _patterns.ListAsync(cancellationToken);
            return patterns.Count > 0
                ? HealthCheckResult.Healthy($"Loaded {patterns.Count} approved service pattern(s).")
                : HealthCheckResult.Degraded("The service-pattern catalog loaded but contains no approved patterns.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to load the service-pattern catalog.", ex);
        }
    }
}
