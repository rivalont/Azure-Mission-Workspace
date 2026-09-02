using AzureMissionWorkspace.Infrastructure.DependencyInjection;
using AzureMissionWorkspace.Infrastructure.Persistence;
using AzureMissionWorkspace.Worker;

var builder = Host.CreateApplicationBuilder(args);

// The Worker runs entirely on the same in-memory persistence and deterministic fake Azure
// DevOps/Resource Manager adapters as the MCP server. In a production deployment these would be
// replaced with durable, shared implementations (for example Azure Cosmos DB and a real Azure
// DevOps client) without changing the Worker's own logic.
builder.Services.AddAzureMissionWorkspaceInfrastructure(builder.Configuration);

builder.Services.AddSingleton<DeploymentEvidenceFinalizer>();

builder.Services.AddOptions<PipelineReconciliationOptions>().Bind(builder.Configuration.GetSection("PipelineReconciliation"));
builder.Services.AddOptions<ApprovalExpirationOptions>().Bind(builder.Configuration.GetSection("ApprovalExpiration"));

builder.Services.AddHostedService<PipelineStatusReconciliationService>();
builder.Services.AddHostedService<ApprovalExpirationService>();

var host = builder.Build();
host.Run();
