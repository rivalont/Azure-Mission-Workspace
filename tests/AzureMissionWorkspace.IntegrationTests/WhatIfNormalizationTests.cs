using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Infrastructure.Bicep;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMissionWorkspace.IntegrationTests;

public sealed class WhatIfNormalizationTests
{
    public static IEnumerable<object[]> Scenarios()
    {
        yield return ["create-only", WhatIfFixtures.CreateOnly, PlanChangeType.Create, DeploymentRisk.Low, false, false];
        yield return ["safe-modification", WhatIfFixtures.SafeModification, PlanChangeType.Modify, DeploymentRisk.Low, false, false];
        yield return ["resource-deletion", WhatIfFixtures.ResourceDeletion, PlanChangeType.Delete, DeploymentRisk.High, true, false];
        yield return ["resource-replacement", WhatIfFixtures.ResourceReplacement, PlanChangeType.Replace, DeploymentRisk.High, true, false];
        yield return ["unknown-change", WhatIfFixtures.UnknownChange, PlanChangeType.Unknown, DeploymentRisk.ReviewRequired, false, true];
        yield return ["no-change", WhatIfFixtures.NoChange, PlanChangeType.NoChange, DeploymentRisk.Low, false, false];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void WhatIfResultNormalizer_normalizes_expected_change_flags(
        string scenarioName,
        string fixture,
        PlanChangeType expectedChangeType,
        DeploymentRisk expectedRisk,
        bool expectDestructive,
        bool expectUnknown)
    {
        using var serviceProvider = TestHost.BuildServiceProvider();
        var normalizer = serviceProvider.GetRequiredService<IWhatIfResultNormalizer>();

        var plan = normalizer.Normalize(Guid.NewGuid(), new RawWhatIfResult(fixture), EnvironmentType.Development);

        plan.Changes.Should().ContainSingle(because: scenarioName);
        plan.Changes.Single().ChangeType.Should().Be(expectedChangeType);
        plan.OverallRisk.Should().Be(expectedRisk);
        plan.HasDestructiveChanges.Should().Be(expectDestructive);
        plan.HasUnknownChanges.Should().Be(expectUnknown);
    }
}
