using GoldSystem.RateEngine.Models;

namespace GoldSystem.RateEngine.Interfaces;

/// <summary>
/// Abstraction for a gold rate data source (e.g. MCX web scraper, Metals-API).
/// </summary>
public interface IRateSource
{
    /// <summary>Fetches the latest gold rate from the underlying source.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="GoldRateResult"/> or <c>null</c> if the rate could not be retrieved.</returns>
    Task<GoldRateResult?> FetchLatestRateAsync(CancellationToken ct = default);
}
