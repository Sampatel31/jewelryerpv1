using GoldSystem.RateEngine.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="McxRateScraper"/> focusing on HTML parsing logic
/// without making real network calls.
/// </summary>
public class McxRateScraperTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static string BuildHtml(string commodityName, string rateValue) =>
        $"""
        <html><body>
          <table>
            <tr><th>Commodity</th><th>Rate</th></tr>
            <tr><td>{commodityName}</td><td>{rateValue}</td></tr>
          </table>
        </body></html>
        """;

    // ── ParseFromHtml tests ───────────────────────────────────────────────────

    [Fact]
    public void ParseFromHtml_ValidGoldRow_ReturnsParsedRates()
    {
        // Arrange – MCX typically formats rates with commas, e.g. "61,234"
        var html = BuildHtml("GOLD", "61,234");

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(61234m, result.Rate24K);
        Assert.Equal("MCX_SCRAPER", result.Source);
    }

    [Fact]
    public void ParseFromHtml_CalculatesCorrectKaratRates()
    {
        // Arrange
        var html = BuildHtml("GOLD", "72000");

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert – 22K = 72000 × 22/24 = 66000, 18K = 72000 × 18/24 = 54000
        Assert.NotNull(result);
        Assert.Equal(72000m, result.Rate24K);
        Assert.Equal(66000m, result.Rate22K);
        Assert.Equal(54000m, result.Rate18K);
    }

    [Fact]
    public void ParseFromHtml_CaseInsensitiveGoldMatch_Succeeds()
    {
        // Arrange – lowercase commodity name
        var html = BuildHtml("gold", "60000");

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60000m, result.Rate24K);
    }

    [Fact]
    public void ParseFromHtml_NoGoldRow_ReturnsNull()
    {
        // Arrange – table contains SILVER but not GOLD
        var html = BuildHtml("SILVER", "80000");

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFromHtml_EmptyHtml_ReturnsNull()
    {
        // Act
        var result = McxRateScraper.ParseFromHtml(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFromHtml_MalformedHtml_ReturnsNull()
    {
        // Arrange – no table, just random text
        const string html = "<html><body>No table here</body></html>";

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFromHtml_NonNumericRate_ReturnsNull()
    {
        // Arrange
        var html = BuildHtml("GOLD", "N/A");

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFromHtml_ZeroRate_ReturnsNull()
    {
        // Arrange – a zero price is invalid
        var html = BuildHtml("GOLD", "0");

        // Act
        var result = McxRateScraper.ParseFromHtml(html);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFromHtml_Source_IsAlwaysMcxScraper()
    {
        var html = BuildHtml("GOLD", "70000");
        var result = McxRateScraper.ParseFromHtml(html);
        Assert.Equal("MCX_SCRAPER", result?.Source);
    }

    [Fact]
    public void ParseFromHtml_FetchedAt_IsReasonablyCloseToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-2);
        var html = BuildHtml("GOLD", "70000");
        var result = McxRateScraper.ParseFromHtml(html);
        var after = DateTime.UtcNow.AddSeconds(2);

        Assert.NotNull(result);
        Assert.InRange(result.FetchedAt, before, after);
    }

    // ── Constructor / DI test ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new McxRateScraper(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_DoesNotThrow()
    {
        var ex = Record.Exception(() => new McxRateScraper(NullLogger<McxRateScraper>.Instance));
        Assert.Null(ex);
    }
}
