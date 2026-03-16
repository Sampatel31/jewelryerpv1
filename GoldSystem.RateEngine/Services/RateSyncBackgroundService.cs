using GoldSystem.RateEngine.Interfaces;
using GoldSystem.RateEngine.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Background service that runs on the owner-branch PC.
/// Polls the primary rate source every <see cref="GoldRateOptions.RefreshIntervalMinutes"/> minutes,
/// saves changed rates to the database, broadcasts them over LAN, and raises stale-rate alerts.
/// </summary>
public sealed class RateSyncBackgroundService : BackgroundService
{
    private readonly IRateSource _primarySource;
    private readonly RateRepository _rateRepository;
    private readonly RateBroadcaster _broadcaster;
    private readonly RateChangedEventPublisher _publisher;
    private readonly IOptions<GoldRateOptions> _options;
    private readonly ILogger<RateSyncBackgroundService> _logger;

    public RateSyncBackgroundService(
        IRateSource primarySource,
        RateRepository rateRepository,
        RateBroadcaster broadcaster,
        RateChangedEventPublisher publisher,
        IOptions<GoldRateOptions> options,
        ILogger<RateSyncBackgroundService> logger)
    {
        _primarySource = primarySource;
        _rateRepository = rateRepository;
        _broadcaster = broadcaster;
        _publisher = publisher;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RateSyncBackgroundService starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollOnceAsync(stoppingToken);

            var delay = TimeSpan.FromMinutes(_options.Value.RefreshIntervalMinutes);
            _logger.LogDebug("Next MCX poll in {Minutes} minutes.", delay.TotalMinutes);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("RateSyncBackgroundService stopped.");
    }

    internal async Task PollOnceAsync(CancellationToken ct)
    {
        try
        {
            var ownerBranchId = _options.Value.OwnerBranchId;
            var latest = await _rateRepository.GetLatestRateAsync(ownerBranchId, ct);
            CheckForStaleRate(latest?.CreatedAt);

            var newRate = await _primarySource.FetchLatestRateAsync(ct);
            if (newRate is null)
            {
                _logger.LogWarning("Primary rate source returned null.");
                return;
            }

            // Only persist and broadcast if the 24 K rate has actually changed.
            if (latest is not null && latest.Rate24K == newRate.Rate24K)
            {
                _logger.LogDebug("24 K rate unchanged ({Rate}); skipping save.", newRate.Rate24K);
                return;
            }

            await _rateRepository.SaveRateAsync(newRate, ownerBranchId, ct: ct);
            _broadcaster.Broadcast(newRate, ownerBranchId);
            _publisher.Publish(new RateChangeEvent
            {
                Rate = newRate,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "MCX_SCRAPER"
            });

            _logger.LogInformation("New rate published: 24K={Rate}", newRate.Rate24K);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during rate poll.");
        }
    }

    private void CheckForStaleRate(DateTime? lastFetchedAt)
    {
        if (lastFetchedAt is null) return;

        var age = DateTime.UtcNow - lastFetchedAt.Value;
        var opts = _options.Value;

        if (age.TotalMinutes >= opts.StaleBlockingMinutes)
        {
            _logger.LogError(
                "STALE RATE – last rate is {Age:F0} minutes old (>{Threshold} min). New bills BLOCKED.",
                age.TotalMinutes, opts.StaleBlockingMinutes);
        }
        else if (age.TotalMinutes >= opts.StaleWarningMinutes)
        {
            _logger.LogWarning(
                "STALE RATE WARNING – last rate is {Age:F0} minutes old (>{Threshold} min).",
                age.TotalMinutes, opts.StaleWarningMinutes);
        }
    }
}
