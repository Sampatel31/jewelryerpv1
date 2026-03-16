namespace GoldSystem.RateEngine;

/// <summary>
/// Strongly-typed configuration options for the Gold Rate Engine.
/// Bound from the "GoldRateConfiguration" section of appsettings.json.
/// </summary>
public class GoldRateOptions
{
    public const string SectionName = "GoldRateConfiguration";

    /// <summary>Primary rate source: "MCX_SCRAPER" (default).</summary>
    public string PrimarySource { get; set; } = "MCX_SCRAPER";

    /// <summary>How often (in minutes) the background service polls for new rates.</summary>
    public int RefreshIntervalMinutes { get; set; } = 5;

    /// <summary>Minutes without a fresh rate before a warning banner is shown.</summary>
    public int StaleWarningMinutes { get; set; } = 30;

    /// <summary>Minutes without a fresh rate before new bills are blocked.</summary>
    public int StaleBlockingMinutes { get; set; } = 120;

    /// <summary>Percentage change that triggers an alert notification.</summary>
    public double AlertOnChangePercent { get; set; } = 2.0;

    /// <summary>Whether to fall back to a secondary source when the primary fails.</summary>
    public bool EnableSecondarySource { get; set; } = false;

    /// <summary>Secondary source name: "METALS_API".</summary>
    public string SecondarySource { get; set; } = "METALS_API";

    /// <summary>
    /// BranchId of the owner (head-office) PC that runs the MCX poller and broadcaster.
    /// Must be configured to match the Branch.BranchId seeded in the database.
    /// </summary>
    public int OwnerBranchId { get; set; } = 1;
}

/// <summary>
/// Configuration options for the UDP rate broadcast.
/// Bound from the "RateBroadcast" section of appsettings.json.
/// </summary>
public class RateBroadcastOptions
{
    public const string SectionName = "RateBroadcast";

    /// <summary>UDP port used for LAN broadcasts.</summary>
    public int Port { get; set; } = 9876;

    /// <summary>Whether broadcasting is enabled.</summary>
    public bool EnableBroadcast { get; set; } = true;
}
