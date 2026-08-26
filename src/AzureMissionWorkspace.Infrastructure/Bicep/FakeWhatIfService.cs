using System.Text.Json;
using AzureMissionWorkspace.Application.Abstractions.Bicep;

namespace AzureMissionWorkspace.Infrastructure.Bicep;

/// <summary>
/// Deterministic fake <see cref="IWhatIfService"/> for local development and tests. Selects one of
/// the representative <see cref="WhatIfFixtures"/> based on an optional <c>whatIfScenario</c> hint
/// found in the rendered parameters JSON, defaulting to a create-only scenario. No Azure credentials
/// or network access are required.
/// </summary>
public sealed class FakeWhatIfService : IWhatIfService
{
    public Task<RawWhatIfResult> ExecuteWhatIfAsync(string compiledTemplateJson, string renderedParametersJson, CancellationToken cancellationToken = default)
    {
        var scenario = TryExtractScenario(renderedParametersJson);

        var rawJson = scenario switch
        {
            "safe-modification" => WhatIfFixtures.SafeModification,
            "resource-deletion" => WhatIfFixtures.ResourceDeletion,
            "resource-replacement" => WhatIfFixtures.ResourceReplacement,
            "unknown-change" => WhatIfFixtures.UnknownChange,
            "no-change" => WhatIfFixtures.NoChange,
            _ => WhatIfFixtures.CreateOnly,
        };

        return Task.FromResult(new RawWhatIfResult(rawJson));
    }

    private static string? TryExtractScenario(string renderedParametersJson)
    {
        try
        {
            using var document = JsonDocument.Parse(renderedParametersJson);
            if (document.RootElement.TryGetProperty("whatIfScenario", out var scenarioElement))
            {
                return scenarioElement.GetString();
            }
        }
        catch (JsonException)
        {
            // The rendered artifact is a .bicepparam file, not JSON, in the common case; the scenario
            // hint is only meaningful for tests that supply a JSON representation directly.
        }

        return null;
    }
}
