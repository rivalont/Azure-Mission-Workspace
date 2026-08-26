using AzureMissionWorkspace.Application.Abstractions.AzureDevOps;
using AzureMissionWorkspace.Application.Abstractions.Bicep;
using AzureMissionWorkspace.Application.Abstractions.Policy;
using AzureMissionWorkspace.Application.Abstractions.Repositories;
using AzureMissionWorkspace.Application.Abstractions.Services;
using AzureMissionWorkspace.Infrastructure.AzureDevOps;
using AzureMissionWorkspace.Infrastructure.Bicep;
using AzureMissionWorkspace.Infrastructure.Deployment;
using AzureMissionWorkspace.Infrastructure.Persistence;
using AzureMissionWorkspace.Infrastructure.Services;
using AzureMissionWorkspace.PolicyEngine;
using AzureMissionWorkspace.PolicyEngine.Abstractions;
using AzureMissionWorkspace.ServicePatterns.Loading;
using AzureMissionWorkspace.ServicePatterns.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMissionWorkspace.Infrastructure.DependencyInjection;

/// <summary>
/// Wires up the starter solution's in-memory persistence and fake/deterministic infrastructure
/// adapters. This is the composition seam a production deployment would replace piece by piece
/// (for example, substituting real Azure Resource Manager and Azure DevOps clients) without
/// changing the domain or application layers.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAzureMissionWorkspaceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServicePatternCatalogOptions>().Bind(configuration.GetSection("ServicePatternCatalog"));
        services.AddOptions<EnvironmentProfileOptions>().Bind(configuration.GetSection("EnvironmentProfiles"));

        // Persistence -- in-memory implementations suitable for local development and tests. The
        // repository interfaces are designed so Azure Cosmos DB, Azure SQL, or another approved
        // store can be substituted later without changing the domain or application layers.
        services.AddSingleton<InMemoryDeploymentRequestRepository>();
        services.AddSingleton<IDeploymentRequestRepository>(sp => sp.GetRequiredService<InMemoryDeploymentRequestRepository>());

        services.AddSingleton<FileSystemServicePatternRepository>();
        services.AddSingleton<IServicePatternRepository>(sp => sp.GetRequiredService<FileSystemServicePatternRepository>());

        services.AddSingleton<FileSystemEnvironmentProfileRepository>();
        services.AddSingleton<IEnvironmentProfileRepository>(sp => sp.GetRequiredService<FileSystemEnvironmentProfileRepository>());

        services.AddSingleton<InMemoryApprovalRepository>();
        services.AddSingleton<IApprovalRepository>(sp => sp.GetRequiredService<InMemoryApprovalRepository>());

        services.AddSingleton<InMemoryDeploymentEvidenceRepository>();
        services.AddSingleton<IDeploymentEvidenceRepository>(sp => sp.GetRequiredService<InMemoryDeploymentEvidenceRepository>());

        // Bicep pipeline abstractions -- deterministic fakes; no Bicep CLI or Azure credentials required locally.
        services.AddSingleton<IBicepParameterRenderer, DeterministicBicepParameterRenderer>();
        services.AddSingleton<IBicepCompiler, FakeBicepCompiler>();
        services.AddSingleton<IBicepLinter, FakeBicepLinter>();
        services.AddSingleton<IBicepModuleResolver, DescriptorBackedBicepModuleResolver>();
        services.AddSingleton<IBicepRegistryClient, FakeBicepRegistryClient>();
        services.AddSingleton<ITemplateValidationService, FakeTemplateValidationService>();
        services.AddSingleton<IWhatIfService, FakeWhatIfService>();
        services.AddSingleton<IWhatIfResultNormalizer, WhatIfResultNormalizer>();

        // Deployment execution strategies -- deterministic fakes standing in for real ARM / Deployment Stacks adapters.
        services.AddSingleton<IDeploymentStrategy, ArmTemplateDeploymentStrategy>();
        services.AddSingleton<IDeploymentStrategy, DeploymentStackStrategy>();
        services.AddSingleton<IDeploymentService, DeploymentService>();

        // Azure DevOps -- a single deterministic fake client backing every sub-service.
        services.AddSingleton<FakeAzureDevOpsClient>();
        services.AddSingleton<IAzureDevOpsClient>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());
        services.AddSingleton<IRepositoryService>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());
        services.AddSingleton<IPullRequestService>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());
        services.AddSingleton<IPipelineService>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());
        services.AddSingleton<IApprovalService>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());
        services.AddSingleton<IArtifactService>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());

        // Optional AI-adjacent abstractions -- deterministic fakes; no external AI service is required.
        services.AddSingleton<IIntentExtractionService, FakeIntentExtractionService>();
        services.AddSingleton<IPatternRecommendationService, CatalogPatternRecommendationService>();
        services.AddSingleton<IServiceAvailabilityProvider, IndeterminateServiceAvailabilityProvider>();
        services.AddSingleton<IExplanationService, DeterministicExplanationService>();

        // Policy engine.
        services.AddSingleton<IPolicyCatalog, StaticPolicyCatalog>();
        services.AddSingleton<IPolicyResultNormalizer, PolicyResultNormalizer>();
        services.AddSingleton<IPolicyEvaluator, DeterministicPolicyEvaluator>();

        return services;
    }
}
