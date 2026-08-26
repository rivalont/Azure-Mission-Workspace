using System.ComponentModel;
using System.Text.Json;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.McpServer.Dtos;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Resources;

/// <summary>
/// Read-only MCP resources exposing Azure Mission Workspace entities by canonical
/// <c>mission-workspace://</c> URI. All resources are read-only projections; mutation only ever
/// happens through the corresponding MCP tools, never through resource reads.
/// </summary>
[McpServerResourceType]
public sealed class MissionWorkspaceResources
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IServicePatternRepository _patterns;
    private readonly IDeploymentRequestRepository _requests;
    private readonly IDeploymentEvidenceRepository _evidence;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly Tools.IPlanAndPolicyCache _cache;

    public MissionWorkspaceResources(
        IServicePatternRepository patterns,
        IDeploymentRequestRepository requests,
        IDeploymentEvidenceRepository evidence,
        IEnvironmentProfileRepository environmentProfiles,
        Tools.IPlanAndPolicyCache cache)
    {
        _patterns = patterns;
        _requests = requests;
        _evidence = evidence;
        _environmentProfiles = environmentProfiles;
        _cache = cache;
    }

    [McpServerResource(UriTemplate = "mission-workspace://service-patterns/{id}/{version}", Name = "service-pattern", MimeType = "application/json")]
    [Description("Read-only. An approved service-pattern descriptor by id and version.")]
    public async Task<string> GetServicePatternResourceAsync(string id, string version, CancellationToken cancellationToken)
    {
        var pattern = await _patterns.FindAsync(new ServicePatternId(id), new ServicePatternVersion(version), cancellationToken);
        return pattern is null
            ? JsonSerializer.Serialize(new { error = $"Service pattern '{id}@{version}' was not found." }, SerializerOptions)
            : JsonSerializer.Serialize(ServicePatternDto.FromDomain(pattern), SerializerOptions);
    }

    [McpServerResource(UriTemplate = "mission-workspace://deployment-requests/{id}", Name = "deployment-request", MimeType = "application/json")]
    [Description("Read-only. The current state of a deployment request by id.")]
    public async Task<string> GetDeploymentRequestResourceAsync(string id, CancellationToken cancellationToken)
    {
        var request = await _requests.FindByIdAsync(new DeploymentRequestId(Guid.Parse(id)), cancellationToken);
        return request is null
            ? JsonSerializer.Serialize(new { error = $"Deployment request '{id}' was not found." }, SerializerOptions)
            : JsonSerializer.Serialize(DeploymentRequestDto.FromDomain(request), SerializerOptions);
    }

    [McpServerResource(UriTemplate = "mission-workspace://deployment-plans/{id}", Name = "deployment-plan", MimeType = "application/json")]
    [Description("Read-only. The most recently generated normalized deployment plan for a deployment request.")]
    public string GetDeploymentPlanResource(string id)
    {
        var plan = _cache.GetPlan(Guid.Parse(id));
        return plan is null
            ? JsonSerializer.Serialize(new { error = $"No deployment plan has been generated yet for deployment request '{id}'." }, SerializerOptions)
            : JsonSerializer.Serialize(DeploymentPlanDto.FromDomain(plan), SerializerOptions);
    }

    [McpServerResource(UriTemplate = "mission-workspace://deployment-evidence/{id}", Name = "deployment-evidence", MimeType = "application/json")]
    [Description("Read-only. The finalized, hash-verifiable deployment evidence package for a deployment request, if available. Secrets are never included.")]
    public async Task<string> GetDeploymentEvidenceResourceAsync(string id, CancellationToken cancellationToken)
    {
        var evidence = await _evidence.FindByDeploymentRequestIdAsync(new DeploymentRequestId(Guid.Parse(id)), cancellationToken);
        return evidence is null
            ? JsonSerializer.Serialize(new { error = $"No finalized evidence is available yet for deployment request '{id}'." }, SerializerOptions)
            : JsonSerializer.Serialize(DeploymentEvidenceDto.FromDomain(evidence), SerializerOptions);
    }

    [McpServerResource(UriTemplate = "mission-workspace://environment-profiles/{id}", Name = "environment-profile", MimeType = "application/json")]
    [Description("Read-only. A platform-administrator-controlled environment profile by id. Environment profiles cannot be created or modified through MCP.")]
    public async Task<string> GetEnvironmentProfileResourceAsync(string id, CancellationToken cancellationToken)
    {
        var profile = await _environmentProfiles.FindByIdAsync(new EnvironmentProfileId(id), cancellationToken);
        return profile is null
            ? JsonSerializer.Serialize(new { error = $"Environment profile '{id}' was not found." }, SerializerOptions)
            : JsonSerializer.Serialize(new
            {
                profile.Id.Value,
                Cloud = profile.Cloud.ToString(),
                EnvironmentType = profile.EnvironmentType.ToString(),
                profile.DefaultLocation,
                profile.AllowedLocations,
                AllowedServicePatterns = profile.AllowedServicePatterns.Select(p => p.Value),
                profile.RequiredTags,
                profile.RequiresApprovalForProduction,
            }, SerializerOptions);
    }
}
