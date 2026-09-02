using System.Text.Json;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.ValueObjects;

namespace AzureMissionWorkspace.Infrastructure.Persistence;

/// <summary>Loads <see cref="EnvironmentProfile"/> entities from the repository's <c>environment-profiles/*.json</c> files.</summary>
public static class EnvironmentProfileJsonLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyCollection<EnvironmentProfile> LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        var profiles = new List<EnvironmentProfile>();

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var json = File.ReadAllText(file);
            var model = JsonSerializer.Deserialize<EnvironmentProfileJsonModel>(json, SerializerOptions)
                ?? throw new InvalidOperationException($"Environment profile file '{file}' could not be parsed.");

            profiles.Add(ToDomain(model));
        }

        return profiles;
    }

    private static EnvironmentProfile ToDomain(EnvironmentProfileJsonModel model)
    {
        var environmentType = InferEnvironmentType(model.Id);

        var requiredTags = model.RequiredTags.ToDictionary(tag => tag, _ => "required", StringComparer.OrdinalIgnoreCase);

        return new EnvironmentProfile(
            new EnvironmentProfileId(model.Id),
            Enum.Parse<AzureCloud>(model.Cloud, ignoreCase: true),
            environmentType,
            model.TenantId,
            model.SubscriptionId,
            model.DefaultLocation,
            model.AllowedLocations,
            model.AllowedServicePatterns.Select(id => new ServicePatternId(id)).ToArray(),
            requiredTags,
            requiresApprovalForProduction: environmentType == EnvironmentType.Production);
    }

    /// <summary>
    /// Environment profile IDs follow the convention <c>{cloud}-{tenant-scope}-{environmentType}</c>
    /// (for example <c>azure-commercial-development</c>); the environment type is the final
    /// hyphen-delimited segment.
    /// </summary>
    private static EnvironmentType InferEnvironmentType(string profileId)
    {
        var lastSegment = profileId.Split('-').Last();
        return Enum.TryParse<EnvironmentType>(lastSegment, ignoreCase: true, out var parsed)
            ? parsed
            : EnvironmentType.Development;
    }
}
