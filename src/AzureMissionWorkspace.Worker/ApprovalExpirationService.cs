using AzureMissionWorkspace.Domain.Enums;
using AzureMissionWorkspace.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AzureMissionWorkspace.Worker;

public sealed class ApprovalExpirationOptions
{
    /// <summary>How long a deployment request may remain in AwaitingApproval before it expires.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(24);

    /// <summary>How often the Worker sweeps for expired approvals.</summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Periodically expires deployment requests that have sat in <see cref="DeploymentRequestStatus.AwaitingApproval"/>
/// for longer than the configured timeout, so that stale, unattended approval requests do not
/// remain open indefinitely. Expiration is a system-driven, deterministic time-based transition --
/// it never grants, denies, or bypasses an approval decision itself.
/// </summary>
public sealed class ApprovalExpirationService : BackgroundService
{
    private readonly InMemoryDeploymentRequestRepository _requests;
    private readonly ILogger<ApprovalExpirationService> _logger;
    private readonly ApprovalExpirationOptions _options;

    public ApprovalExpirationService(
        InMemoryDeploymentRequestRepository requests,
        IOptions<ApprovalExpirationOptions> options,
        ILogger<ApprovalExpirationService> logger)
    {
        _requests = requests;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var awaitingApproval = _requests.ListByStatus(DeploymentRequestStatus.AwaitingApproval);
            foreach (var request in awaitingApproval)
            {
                if (DateTimeOffset.UtcNow - request.UpdatedAtUtc < _options.Timeout)
                {
                    continue;
                }

                request.TransitionTo(DeploymentRequestStatus.Expired, WorkerActor.Identity, request.Version);
                await _requests.SaveAsync(request, stoppingToken);
                _logger.LogInformation("Deployment request {DeploymentRequestId} expired after exceeding the approval timeout.", request.Id.Value);
            }

            try
            {
                await Task.Delay(_options.SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
