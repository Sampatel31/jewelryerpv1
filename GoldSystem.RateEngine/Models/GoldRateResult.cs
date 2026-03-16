namespace GoldSystem.RateEngine.Models;

/// <summary>
/// Immutable record representing a fetched gold rate result.
/// </summary>
/// <param name="Rate24K">Gold rate per 10g for 24K (999) purity.</param>
/// <param name="Rate22K">Gold rate per 10g for 22K (916) purity.</param>
/// <param name="Rate18K">Gold rate per 10g for 18K (750) purity.</param>
/// <param name="Source">Source identifier: MCX_SCRAPER, MANUAL_OVERRIDE, or LAN_BROADCAST.</param>
/// <param name="FetchedAt">UTC timestamp when the rate was fetched.</param>
public record GoldRateResult(
    decimal Rate24K,
    decimal Rate22K,
    decimal Rate18K,
    string Source,
    DateTime FetchedAt);
