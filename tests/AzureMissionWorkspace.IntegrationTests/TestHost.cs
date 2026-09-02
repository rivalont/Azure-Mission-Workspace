using AzureMissionWorkspace.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMissionWorkspace.IntegrationTests;

internal static class TestHost
{
    public static ServiceProvider BuildServiceProvider()
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServicePatternCatalog:CatalogRootPath"] = Path.Combine(repositoryRoot, "service-patterns"),
                ["EnvironmentProfiles:ProfilesRootPath"] = Path.Combine(repositoryRoot, "environment-profiles"),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddAzureMissionWorkspaceInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    public static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AzureMissionWorkspace.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
