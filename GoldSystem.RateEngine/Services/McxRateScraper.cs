using GoldSystem.RateEngine.Interfaces;
using GoldSystem.RateEngine.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Fetches the live gold spot price from the MCX India website.
/// MCX publishes prices per 10 g for 999 (24 K equivalent) purity.
/// </summary>
public class McxRateScraper : IRateSource
{
    private const string McxUrl = "https://www.mcxindia.com/market-data/spot-market-data";
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<McxRateScraper> _logger;

    public McxRateScraper(ILogger<McxRateScraper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<GoldRateResult?> FetchLatestRateAsync(CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("MCX rate fetch attempt {Attempt}/{Max}", attempt, MaxRetries);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(RequestTimeout);

                var rate24K = await FetchRateFromMcxAsync(cts.Token);
                if (rate24K is null)
                {
                    _logger.LogWarning("GOLD row not found in MCX page on attempt {Attempt}", attempt);
                }
                else
                {
                    var result = BuildResult(rate24K.Value);
                    _logger.LogInformation(
                        "MCX fetch succeeded: 24K={Rate24K}, 22K={Rate22K}, 18K={Rate18K}",
                        result.Rate24K, result.Rate22K, result.Rate18K);
                    return result;
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("MCX request timed out on attempt {Attempt}", attempt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCX fetch failed on attempt {Attempt}", attempt);
            }

            if (attempt < MaxRetries)
                await Task.Delay(RetryDelay, ct);
        }

        _logger.LogError("MCX rate fetch failed after {MaxRetries} attempts", MaxRetries);
        return null;
    }

    // Public for testing – accepts raw HTML so tests can inject mock markup.
    public static GoldRateResult? ParseFromHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Look for a table row that contains "GOLD" as the commodity name.
        var rows = doc.DocumentNode.SelectNodes("//table//tr");
        if (rows is null) return null;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("td");
            if (cells is null || cells.Count < 2) continue;

            var commodity = cells[0].InnerText.Trim();
            if (!commodity.Contains("GOLD", StringComparison.OrdinalIgnoreCase)) continue;

            // The rate column is the second <td> (index 1).
            var rateText = cells[1].InnerText.Trim();
            if (TryParseRate(rateText, out decimal rate24K))
                return BuildResult(rate24K);
        }

        return null;
    }

    private async Task<decimal?> FetchRateFromMcxAsync(CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/120.0.0.0 Safari/537.36");

        var html = await client.GetStringAsync(McxUrl, ct);

        var result = ParseFromHtml(html);
        return result?.Rate24K;
    }

    private static GoldRateResult BuildResult(decimal rate24K) =>
        new(
            Rate24K: rate24K,
            Rate22K: Math.Round(rate24K * 22m / 24m, 2),
            Rate18K: Math.Round(rate24K * 18m / 24m, 2),
            Source: "MCX_SCRAPER",
            FetchedAt: DateTime.UtcNow);

    private static bool TryParseRate(string text, out decimal value)
    {
        // Remove commas, spaces, currency symbols and try parsing.
        var cleaned = text.Replace(",", "").Replace(" ", "").Trim();
        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0;
    }
}
