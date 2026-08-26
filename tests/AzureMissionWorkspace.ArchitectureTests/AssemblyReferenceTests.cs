using AzureMissionWorkspace.Application.UseCases.DeploymentRequests;
using AzureMissionWorkspace.Domain.Entities;
using AzureMissionWorkspace.Infrastructure.DependencyInjection;
using AzureMissionWorkspace.PolicyEngine;
using AzureMissionWorkspace.ServicePatterns.Loading;
using FluentAssertions;

namespace AzureMissionWorkspace.ArchitectureTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void Domain_does_not_reference_forbidden_platform_assemblies()
    {
        var references = GetReferenceNames(typeof(DeploymentRequest).Assembly);

        references.Should().NotContain(name => StartsWithAny(name, "Azure.", "ModelContextProtocol.", "Microsoft.EntityFrameworkCore", "Microsoft.Azure.", "Microsoft.TeamFoundation."));
    }

    [Fact]
    public void Application_does_not_reference_azure_or_mcp_assemblies()
    {
        var references = GetReferenceNames(typeof(CreateDeploymentRequestHandler).Assembly);

        references.Should().Contain("AzureMissionWorkspace.Domain");
        references.Should().NotContain(name => StartsWithAny(name, "Azure.", "ModelContextProtocol."));
    }

    [Fact]
    public void Project_layering_references_match_expected_dependencies()
    {
        var domainReferences = GetReferenceNames(typeof(DeploymentRequest).Assembly);
        var applicationReferences = GetReferenceNames(typeof(CreateDeploymentRequestHandler).Assembly);
        var servicePatternReferences = GetReferenceNames(typeof(FileSystemServicePatternRepository).Assembly);
        var policyEngineReferences = GetReferenceNames(typeof(StaticPolicyCatalog).Assembly);
        var infrastructureReferences = GetReferenceNames(typeof(InfrastructureServiceCollectionExtensions).Assembly);

        domainReferences.Should().NotContain(name => StartsWithAny(name,
            "AzureMissionWorkspace.Application",
            "AzureMissionWorkspace.ServicePatterns",
            "AzureMissionWorkspace.PolicyEngine",
            "AzureMissionWorkspace.Infrastructure"));

        applicationReferences.Should().Contain("AzureMissionWorkspace.Domain");
        applicationReferences.Should().NotContain(name => StartsWithAny(name,
            "AzureMissionWorkspace.Infrastructure",
            "AzureMissionWorkspace.ServicePatterns",
            "AzureMissionWorkspace.PolicyEngine"));

        servicePatternReferences.Should().Contain(["AzureMissionWorkspace.Domain", "AzureMissionWorkspace.Application"]);
        servicePatternReferences.Should().NotContain(name => StartsWithAny(name,
            "AzureMissionWorkspace.PolicyEngine",
            "AzureMissionWorkspace.Infrastructure"));

        policyEngineReferences.Should().Contain(["AzureMissionWorkspace.Domain", "AzureMissionWorkspace.Application"]);
        policyEngineReferences.Should().NotContain(name => StartsWithAny(name,
            "AzureMissionWorkspace.ServicePatterns",
            "AzureMissionWorkspace.Infrastructure"));

        infrastructureReferences.Should().Contain([
            "AzureMissionWorkspace.Domain",
            "AzureMissionWorkspace.Application",
            "AzureMissionWorkspace.ServicePatterns",
            "AzureMissionWorkspace.PolicyEngine"]);
    }

    private static string[] GetReferenceNames(System.Reflection.Assembly assembly)
        => assembly.GetReferencedAssemblies().Select(static a => a.Name ?? string.Empty).ToArray();

    private static bool StartsWithAny(string candidate, params string[] prefixes)
        => prefixes.Any(prefix => candidate.StartsWith(prefix, StringComparison.Ordinal));
}
