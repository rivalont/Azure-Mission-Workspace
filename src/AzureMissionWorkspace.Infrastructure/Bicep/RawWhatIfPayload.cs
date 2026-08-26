using System.Text.Json.Serialization;

namespace AzureMissionWorkspace.Infrastructure.Bicep;

/// <summary>Simplified raw what-if JSON shape produced by <see cref="FakeWhatIfService"/> and consumed by <see cref="WhatIfResultNormalizer"/>.</summary>
public sealed class RawWhatIfChange
{
    [JsonPropertyName("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>One of the <see cref="Domain.Enums.PlanChangeType"/> member names.</summary>
    [JsonPropertyName("changeType")]
    public string ChangeType { get; set; } = "Unknown";

    [JsonPropertyName("changedProperties")]
    public List<string> ChangedProperties { get; set; } = [];
}

public sealed class RawWhatIfPayload
{
    [JsonPropertyName("changes")]
    public List<RawWhatIfChange> Changes { get; set; } = [];
}
