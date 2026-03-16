namespace GoldSystem.RateEngine.Models;

/// <summary>
/// Event raised when a new gold rate becomes available.
/// </summary>
public class RateChangeEvent
{
    /// <summary>The new gold rate that triggered the change.</summary>
    public GoldRateResult Rate { get; set; } = null!;

    /// <summary>UTC timestamp when the change was detected.</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Who/what caused the change.
    /// Possible values: "MCX_SCRAPER", "MANUAL_OVERRIDE", "LAN_BROADCAST".
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
}
