using System.Text;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Domain.Entities;

namespace AzureMissionWorkspace.Infrastructure.Services;

/// <summary>
/// Deterministic, template-based <see cref="IExplanationService"/> implementation. Summarizes the
/// already-computed, deterministic <see cref="DeploymentPlan"/> and <see cref="PolicyFinding"/>
/// results in natural language. The explanation is clearly derived from, and never overrides or
/// replaces, the underlying deterministic findings -- no risk or policy value is assigned here.
/// </summary>
public sealed class DeterministicExplanationService : IExplanationService
{
    public Task<string> ExplainDeploymentPlanAsync(DeploymentPlan plan, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"This deployment plan contains {plan.Changes.Count} resource change(s) with an overall deterministic risk of '{plan.OverallRisk}'.");

        foreach (var group in plan.Changes.GroupBy(c => c.ChangeType))
        {
            builder.AppendLine($"- {group.Count()} {group.Key} change(s).");
        }

        if (plan.HasDestructiveChanges)
        {
            builder.AppendLine("This plan includes destructive changes (delete or replace) that require elevated review.");
        }

        if (plan.HasUnknownChanges)
        {
            builder.AppendLine("This plan includes unknown changes that require manual review before approval.");
        }

        return Task.FromResult(builder.ToString().TrimEnd());
    }

    public Task<string> ExplainPolicyFindingAsync(PolicyFinding finding, CancellationToken cancellationToken = default)
    {
        var explanation = $"[{finding.Severity}] {finding.Title}: {finding.Description} Remediation: {finding.Remediation}";
        return Task.FromResult(explanation);
    }
}
