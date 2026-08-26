using System.ComponentModel;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;
using AzureMissionWorkspace.McpServer.Dtos;
using AzureMissionWorkspace.McpServer.Security;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AzureMissionWorkspace.McpServer.Tools;

/// <summary>
/// MCP tools for browsing the approved service-pattern catalog and recommending a pattern for a
/// natural-language capability description. All operations here are read-only and advisory: any
/// recommendation must still be explicitly selected or accepted by the requestor before further
/// processing occurs (see <c>select_service_pattern</c> / <c>create_deployment_request</c>).
/// </summary>
[McpServerToolType]
public sealed class ServicePatternTools
{
    private readonly IServicePatternRepository _patterns;
    private readonly IEnvironmentProfileRepository _environmentProfiles;
    private readonly IIntentExtractionService _intentExtraction;
    private readonly IPatternRecommendationService _recommendation;

    public ServicePatternTools(
        IServicePatternRepository patterns,
        IEnvironmentProfileRepository environmentProfiles,
        IIntentExtractionService intentExtraction,
        IPatternRecommendationService recommendation)
    {
        _patterns = patterns;
        _environmentProfiles = environmentProfiles;
        _intentExtraction = intentExtraction;
        _recommendation = recommendation;
    }

    [McpServerTool(Name = "list_service_patterns", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Lists every approved service pattern in the catalog, including deprecated ones.")]
    [Authorize(Policy = AuthorizationPolicyNames.ServicePatternReader)]
    public async Task<IReadOnlyCollection<ServicePatternDto>> ListServicePatternsAsync(CancellationToken cancellationToken)
    {
        var patterns = await _patterns.ListAsync(cancellationToken);
        return patterns.Select(ServicePatternDto.FromDomain).ToArray();
    }

    [McpServerTool(Name = "get_service_pattern", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Retrieves a single approved service pattern by id and version.")]
    [Authorize(Policy = AuthorizationPolicyNames.ServicePatternReader)]
    public async Task<ServicePatternDto?> GetServicePatternAsync(
        [Description("The service pattern id, for example 'storage-account'.")] string servicePatternId,
        [Description("The service pattern version, for example '1.0.0'.")] string servicePatternVersion,
        CancellationToken cancellationToken)
    {
        var pattern = await _patterns.FindAsync(new ServicePatternId(servicePatternId), new ServicePatternVersion(servicePatternVersion), cancellationToken);
        return pattern is null ? null : ServicePatternDto.FromDomain(pattern);
    }

    [McpServerTool(Name = "recommend_service_pattern", ReadOnly = true, Idempotent = false)]
    [Description("Read-only and advisory. Scores the approved catalog against a natural-language capability description for a given environment profile. The requestor must still explicitly select a pattern -- this tool never makes the final choice.")]
    [Authorize(Policy = AuthorizationPolicyNames.ServicePatternReader)]
    public async Task<IReadOnlyCollection<ServicePatternRecommendationDto>> RecommendServicePatternAsync(
        [Description("A natural-language description of the desired capability.")] string naturalLanguageRequest,
        [Description("The environment profile id the recommendation should be scoped to.")] string environmentProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await _environmentProfiles.FindByIdAsync(new EnvironmentProfileId(environmentProfileId), cancellationToken)
            ?? throw new KeyNotFoundException($"Environment profile '{environmentProfileId}' was not found.");

        var intent = await _intentExtraction.ExtractAsync(naturalLanguageRequest, cancellationToken);
        var recommendations = await _recommendation.RecommendAsync(intent, profile.EnvironmentType, profile.Cloud, cancellationToken);

        return recommendations
            .Select(r => new ServicePatternRecommendationDto(r.PatternId.Value, r.Version.Value, r.Score, r.Rationale))
            .ToArray();
    }

    [McpServerTool(Name = "get_required_inputs", ReadOnly = true, Idempotent = true)]
    [Description("Read-only. Returns the required and optional inputs a service pattern needs before parameters can be rendered.")]
    [Authorize(Policy = AuthorizationPolicyNames.ServicePatternReader)]
    public async Task<ServicePatternInputsDto> GetRequiredInputsAsync(
        [Description("The service pattern id.")] string servicePatternId,
        [Description("The service pattern version.")] string servicePatternVersion,
        CancellationToken cancellationToken)
    {
        var pattern = await _patterns.FindAsync(new ServicePatternId(servicePatternId), new ServicePatternVersion(servicePatternVersion), cancellationToken)
            ?? throw new KeyNotFoundException($"Service pattern '{servicePatternId}@{servicePatternVersion}' was not found.");

        return new ServicePatternInputsDto(
            pattern.RequiredInputs.Select(i => new ServicePatternInputDto(i.Name, i.Type, i.Description, i.IsRequired, i.IsSecret, i.DefaultValue)).ToArray(),
            pattern.OptionalInputs.Select(i => new ServicePatternInputDto(i.Name, i.Type, i.Description, i.IsRequired, i.IsSecret, i.DefaultValue)).ToArray());
    }
}

public sealed record ServicePatternRecommendationDto(string ServicePatternId, string ServicePatternVersion, double Score, string Rationale);

public sealed record ServicePatternInputsDto(IReadOnlyCollection<ServicePatternInputDto> RequiredInputs, IReadOnlyCollection<ServicePatternInputDto> OptionalInputs);
