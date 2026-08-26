using System.Text.Json.Serialization;

namespace AzureMissionWorkspace.Infrastructure.Persistence;

/// <summary>JSON shape of an <c>environment-profiles/*.json</c> file (see <c>schemas/environment-profile.schema.json</c>).</summary>
public sealed class EnvironmentProfileJsonModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("cloud")]
    public string Cloud { get; set; } = "AzureCommercial";

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [JsonPropertyName("defaultLocation")]
    public string DefaultLocation { get; set; } = string.Empty;

    [JsonPropertyName("allowedLocations")]
    public List<string> AllowedLocations { get; set; } = [];

    [JsonPropertyName("requiredTags")]
    public List<string> RequiredTags { get; set; } = [];

    [JsonPropertyName("allowedServicePatterns")]
    public List<string> AllowedServicePatterns { get; set; } = [];
}
