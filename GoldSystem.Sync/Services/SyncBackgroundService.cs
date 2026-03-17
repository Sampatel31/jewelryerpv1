using GoldSystem.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoldSystem.Sync.Services;

/// <summary>
/// Hosted background service that periodically pushes pending SyncQueue records to the
/// owner database.  The service is intentionally disabled on the owner PC itself.
/// </summary>
public class SyncBackgroundService : BackgroundService
{
    private readonly IUnitOfWork _uow;
    private readonly ISyncPushService _syncPushService;
    private readonly ILogger<SyncBackgroundService> _logger;
    private readonly IConfiguration _config;

    public SyncBackgroundService(
        IUnitOfWork uow,
        ISyncPushService syncPushService,
        ILogger<SyncBackgroundService> logger,
        IConfiguration config)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _syncPushService = syncPushService ?? throw new ArgumentNullException(nameof(syncPushService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var syncIntervalMinutes = _config.GetValue<int>("SyncConfiguration:IntervalMinutes", 15);
        var branchId = GetCurrentBranchId();

        var branch = await _uow.Branches.GetByIdAsync(branchId);

        // Only run on branch PCs, NOT on owner.
        if (branch?.IsOwnerBranch == true)
        {
            _logger.LogInformation("Owner PC detected. Sync service disabled.");
            return;
        }

        _logger.LogInformation(
            "Sync service started for branch {BranchId}. Interval: {Minutes} minutes",
            branchId, syncIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Yield to the scheduler at the start of each iteration so the loop
            // never runs as an infinite synchronous tight-loop, even when the
            // configured interval is zero and all awaited tasks complete immediately.
            await Task.Yield();

            try
            {
                await _syncPushService.PushPendingSyncsAsync(branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sync cycle for branch {BranchId}", branchId);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(syncIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown requested.
                break;
            }
        }
    }

    private static int GetCurrentBranchId()
    {
        var value = Environment.GetEnvironmentVariable("BRANCH_ID");
        return int.TryParse(value, out var id) ? id : 1;
    }
}
