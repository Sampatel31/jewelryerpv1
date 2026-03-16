using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Provides read access to the gold rate engine configuration and supports
/// runtime updates that are reflected immediately without restarting the application.
/// </summary>
public class RateConfigurationService
{
    private readonly IOptionsMonitor<GoldRateOptions> _rateMonitor;
    private readonly IOptionsMonitor<RateBroadcastOptions> _broadcastMonitor;
    private readonly ILogger<RateConfigurationService> _logger;

    public RateConfigurationService(
        IOptionsMonitor<GoldRateOptions> rateMonitor,
        IOptionsMonitor<RateBroadcastOptions> broadcastMonitor,
        ILogger<RateConfigurationService> logger)
    {
        _rateMonitor = rateMonitor;
        _broadcastMonitor = broadcastMonitor;
        _logger = logger;

        _rateMonitor.OnChange(o => _logger.LogInformation(
            "GoldRateOptions updated: PrimarySource={PrimarySource}, RefreshIntervalMinutes={Interval}",
            o.PrimarySource, o.RefreshIntervalMinutes));

        _broadcastMonitor.OnChange(o => _logger.LogInformation(
            "RateBroadcastOptions updated: Port={Port}, EnableBroadcast={Enable}",
            o.Port, o.EnableBroadcast));
    }

    /// <summary>Current gold rate configuration (reflects latest appsettings.json values).</summary>
    public GoldRateOptions RateOptions => _rateMonitor.CurrentValue;

    /// <summary>Current broadcast configuration.</summary>
    public RateBroadcastOptions BroadcastOptions => _broadcastMonitor.CurrentValue;
}
