using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Domain.Enums;

namespace AzureMissionWorkspace.Infrastructure.Services;

/// <summary>
/// Deterministic fake <see cref="IIntentExtractionService"/> for local development and tests. Uses
/// simple keyword matching -- no external AI service is required to compile or run the starter
/// solution. Real implementations may substitute a large-language-model-backed service, but must
/// never bypass downstream validation, policy, or approval controls.
/// </summary>
public sealed class FakeIntentExtractionService : IIntentExtractionService
{
    private static readonly (string Keyword, string Capability)[] KeywordMap =
    [
        ("api", "internal-web-api"),
        ("web app", "internal-web-app"),
        ("website", "internal-web-app"),
        ("storage", "storage-account"),
        ("blob", "storage-account"),
        ("secret", "key-vault"),
        ("vault", "key-vault"),
        ("key vault", "key-vault"),
    ];

    public Task<IntentExtractionResult> ExtractAsync(string naturalLanguageRequest, CancellationToken cancellationToken = default)
    {
        var text = naturalLanguageRequest.ToLowerInvariant();

        var capabilities = KeywordMap
            .Where(entry => text.Contains(entry.Keyword, StringComparison.Ordinal))
            .Select(entry => entry.Capability)
            .Distinct()
            .Select(capability => new ExtractedCapability(capability, new Dictionary<string, string>()))
            .ToArray();

        var notes = capabilities.Length == 0
            ? "No known capability keywords were recognized; manual pattern selection is required."
            : null;

        return Task.FromResult(new IntentExtractionResult(capabilities, notes));
    }
}

/// <summary>
/// Deterministic <see cref="IPatternRecommendationService"/> that scores the approved service-pattern
/// catalog against extracted capabilities and the target environment/cloud compatibility. Purely
/// advisory: the requestor must still explicitly select or accept a recommended pattern.
/// </summary>
public sealed class CatalogPatternRecommendationService : IPatternRecommendationService
{
    private readonly IServicePatternRepository _patternRepository;

    public CatalogPatternRecommendationService(IServicePatternRepository patternRepository)
    {
        _patternRepository = patternRepository;
    }

    public async Task<IReadOnlyCollection<ServicePatternRecommendation>> RecommendAsync(IntentExtractionResult intent, EnvironmentType environmentType, AzureCloud cloud, CancellationToken cancellationToken = default)
    {
        var catalog = await _patternRepository.ListAsync(cancellationToken);
        var requestedCapabilities = intent.Capabilities.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recommendations = new List<ServicePatternRecommendation>();

        foreach (var pattern in catalog)
        {
            if (!pattern.SupportsCloud(cloud) || !pattern.SupportsEnvironmentType(environmentType))
            {
                continue;
            }

            var matches = requestedCapabilities.Contains(pattern.Id.Value);
            var score = matches ? 1.0 : 0.1;
            var rationale = matches
                ? $"Pattern id '{pattern.Id.Value}' matches an extracted capability."
                : "No extracted capability matched this pattern; listed as a fallback option.";

            recommendations.Add(new ServicePatternRecommendation(pattern.Id, pattern.Version, score, rationale));
        }

        return recommendations.OrderByDescending(r => r.Score).ToArray();
    }
}

/// <summary>
/// Deterministic <see cref="IServiceAvailabilityProvider"/> that returns
/// <see cref="ServiceAvailability.Indeterminate"/> for every check. The starter solution does not
/// assume every Azure service, SKU, API version, Azure Policy definition, or Azure Verified Module
/// is available in every cloud; a production implementation should query authoritative Azure
/// resource-provider and policy metadata instead of guessing.
/// </summary>
public sealed class IndeterminateServiceAvailabilityProvider : IServiceAvailabilityProvider
{
    public Task<ServiceAvailability> CheckAvailabilityAsync(AzureCloud cloud, string region, string resourceTypeOrFeature, CancellationToken cancellationToken = default)
        => Task.FromResult(ServiceAvailability.Indeterminate);
}
