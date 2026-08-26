using System.Reflection;
using System.Threading.RateLimiting;
using AzureMissionWorkspace.Application.Abstractions.Authorization;
using AzureMissionWorkspace.Application.Dtos;
using AzureMissionWorkspace.Application.UseCases.Approvals;
using AzureMissionWorkspace.Application.UseCases.DeploymentPlans;
using AzureMissionWorkspace.Application.UseCases.DeploymentRequests;
using AzureMissionWorkspace.Application.UseCases.Deployments;
using AzureMissionWorkspace.Application.UseCases.Policy;
using AzureMissionWorkspace.Application.Validation;
using AzureMissionWorkspace.Infrastructure.DependencyInjection;
using AzureMissionWorkspace.McpServer.Authorization;
using AzureMissionWorkspace.McpServer.Health;
using AzureMissionWorkspace.McpServer.Security;
using AzureMissionWorkspace.McpServer.Tools;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// Core domain/application/infrastructure wiring. The starter solution runs entirely on in-memory
// persistence and deterministic fake Azure Resource Manager / Azure DevOps adapters -- no Azure
// credentials or external services are required to build, run, or exercise this sample.
// ---------------------------------------------------------------------------------------------
builder.Services.AddAzureMissionWorkspaceInfrastructure(builder.Configuration);

builder.Services.AddScoped<CreateDeploymentRequestHandler>();
builder.Services.AddScoped<SelectServicePatternHandler>();
builder.Services.AddScoped<UpdateDeploymentRequestHandler>();
builder.Services.AddScoped<CancelDeploymentRequestHandler>();
builder.Services.AddScoped<ValidateDeploymentRequestHandler>();
builder.Services.AddScoped<GenerateDeploymentPlanHandler>();
builder.Services.AddScoped<EvaluatePolicyComplianceHandler>();
builder.Services.AddScoped<SubmitDeploymentForApprovalHandler>();
builder.Services.AddScoped<RecordApprovalDecisionHandler>();
builder.Services.AddScoped<QueueDeploymentHandler>();
builder.Services.AddScoped<GetDeploymentStatusHandler>();
builder.Services.AddScoped<GetDeploymentEvidenceHandler>();

builder.Services.AddScoped<IValidator<CreateDeploymentRequestInput>, CreateDeploymentRequestInputValidator>();
builder.Services.AddScoped<IValidator<SelectServicePatternInput>, SelectServicePatternInputValidator>();
builder.Services.AddScoped<IValidator<RecordApprovalDecisionInput>, RecordApprovalDecisionInputValidator>();

builder.Services.AddSingleton<IPlanAndPolicyCache, InMemoryPlanAndPolicyCache>();

// ---------------------------------------------------------------------------------------------
// Security: Microsoft Entra ID authentication and ASP.NET Core policy-based authorization. All
// authorization is enforced server-side on every MCP tool invocation -- MCP tool annotations
// (ReadOnly/Destructive/Idempotent) describe intent to clients only and are never trusted as
// security controls.
// ---------------------------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActorProvider, HttpContextCurrentActorProvider>();

var useFakeActor = builder.Configuration.GetValue("Authentication:UseFakeActor", false) && builder.Environment.IsDevelopment();
if (!useFakeActor)
{
    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}
else
{
    // Local-development-only authentication handler that trusts every request as the fake actor
    // defined in HttpContextCurrentActorProvider. Refused outside the Development environment.
    builder.Services.AddAuthentication(FakeDevelopmentAuthenticationHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, FakeDevelopmentAuthenticationHandler>(FakeDevelopmentAuthenticationHandler.SchemeName, _ => { });
}

builder.Services.AddAzureMissionWorkspaceAuthorization();

// ---------------------------------------------------------------------------------------------
// Rate limiting and secure headers.
// ---------------------------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 120),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// ---------------------------------------------------------------------------------------------
// Observability: OpenTelemetry traces and metrics, plus ASP.NET Core health checks.
// ---------------------------------------------------------------------------------------------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("AzureMissionWorkspace.McpServer"))
    .WithTracing(tracing => tracing
        .AddSource("AzureMissionWorkspace.McpServer")
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter("AzureMissionWorkspace.McpServer")
        .AddAspNetCoreInstrumentation());

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<ServicePatternCatalogHealthCheck>("service-pattern-catalog", tags: ["ready"]);

// ---------------------------------------------------------------------------------------------
// Model Context Protocol server: tools, prompts, and resources are registered from this assembly.
// Authorization filters ensure every tool invocation is subject to the policies configured above.
// ---------------------------------------------------------------------------------------------
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "Azure Mission Workspace", Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0" };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly(Assembly.GetExecutingAssembly())
    .WithPromptsFromAssembly(Assembly.GetExecutingAssembly())
    .WithResourcesFromAssembly(Assembly.GetExecutingAssembly())
    .AddAuthorizationFilters();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecureHeadersMiddleware>();

if (!useFakeActor)
{
    app.UseAuthentication();
}

app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapGet("/.well-known/mcp-server-metadata", () => Results.Json(new
{
    name = "Azure Mission Workspace",
    protocol = "model-context-protocol",
    authentication = useFakeActor ? "development-fake-actor" : "microsoft-entra-id",
    transport = "http",
}));

app.MapGet("/audit/correlation/{correlationId}", (string correlationId) =>
    Results.Ok(new { correlationId, message = "Audit event correlation lookup is a starter stub; production deployments should back this with the deployment-evidence audit-events.json artifact." }))
    .RequireAuthorization(AuthorizationPolicyNames.Auditor);

app.MapMcp("/mcp");

app.Run();

/// <summary>Entry point marker used for assembly resolution (MCP tool/prompt/resource scanning) and integration tests.</summary>
public sealed partial class Program;
