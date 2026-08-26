using System.Text.Json;
using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Domain.Services;

namespace AzureMissionWorkspace.Infrastructure.Bicep;

/// <summary>
/// Maps the raw, SDK-independent what-if JSON payload produced by <see cref="FakeWhatIfService"/> (or a
/// real ARM adapter emitting the same shape) into the normalized <see cref="DeploymentPlan"/> model,
/// applying deterministic risk calculation. The raw payload itself is preserved separately in evidence
/// storage; this normalizer never discards it, only derives a structured view from it.
/// </summary>
public sealed class WhatIfResultNormalizer : IWhatIfResultNormalizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public DeploymentPlan Normalize(Guid deploymentRequestId, RawWhatIfResult rawResult, EnvironmentType environmentType)
    {
        var payload = JsonSerializer.Deserialize<RawWhatIfPayload>(rawResult.RawJson, SerializerOptions)
            ?? new RawWhatIfPayload();

        var changes = payload.Changes
            .Select(change =>
            {
                var changeType = Enum.TryParse<PlanChangeType>(change.ChangeType, ignoreCase: true, out var parsed)
                    ? parsed
                    : PlanChangeType.Unknown;

                var risk = DeploymentRiskCalculator.CalculateChangeRisk(changeType, change.ResourceType, environmentType);

                return new DeploymentPlanChange(change.ResourceId, change.ResourceType, changeType, risk, change.ChangedProperties);
            })
            .ToArray();

        var overallRisk = DeploymentRiskCalculator.CalculateOverallRisk(changes.Select(c => c.Risk).ToArray());

        return new DeploymentPlan(Guid.NewGuid(), deploymentRequestId, changes, overallRisk, DateTimeOffset.UtcNow);
    }
}
