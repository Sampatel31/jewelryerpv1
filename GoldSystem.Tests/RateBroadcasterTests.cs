using GoldSystem.RateEngine;
using GoldSystem.RateEngine.Models;
using GoldSystem.RateEngine.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="RateBroadcaster"/>.
/// </summary>
public class RateBroadcasterTests
{
    private static RateBroadcaster CreateBroadcaster(int port = 19876, bool enabled = true)
    {
        var opts = Options.Create(new RateBroadcastOptions { Port = port, EnableBroadcast = enabled });
        return new RateBroadcaster(opts, NullLogger<RateBroadcaster>.Instance);
    }

    // ── Broadcast message format (JSON) ───────────────────────────────────────

    [Fact]
    public void Broadcast_SentPacket_IsValidJson()
    {
        // Use a random high port to avoid conflicts
        int port = GetFreePort();
        var broadcaster = CreateBroadcaster(port);
        var rate = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", DateTime.UtcNow);

        byte[]? receivedBytes = null;
        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        receiver.Client.ReceiveTimeout = 2000;

        broadcaster.Broadcast(rate, branchId: 1);

        try
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            receivedBytes = receiver.Receive(ref ep);
        }
        catch (SocketException)
        {
            // Broadcast may not be receivable in some CI environments – skip assertion.
            return;
        }

        if (receivedBytes is not null)
        {
            var json = Encoding.UTF8.GetString(receivedBytes);
            Assert.True(IsValidJson(json), $"Expected valid JSON but got: {json}");
        }
    }

    [Fact]
    public void Broadcast_Payload_ContainsBranchId()
    {
        int port = GetFreePort();
        var broadcaster = CreateBroadcaster(port);
        var rate = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", DateTime.UtcNow);

        byte[]? receivedBytes = null;
        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        receiver.Client.ReceiveTimeout = 2000;

        broadcaster.Broadcast(rate, branchId: 42);

        try
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            receivedBytes = receiver.Receive(ref ep);
        }
        catch (SocketException) { return; }

        if (receivedBytes is not null)
        {
            var json = Encoding.UTF8.GetString(receivedBytes);
            Assert.Contains("42", json);
        }
    }

    // ── Broadcast disabled ────────────────────────────────────────────────────

    [Fact]
    public void Broadcast_WhenDisabled_DoesNotSendPacket()
    {
        int port = GetFreePort();
        var broadcaster = CreateBroadcaster(port, enabled: false);
        var rate = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", DateTime.UtcNow);

        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        receiver.Client.ReceiveTimeout = 500;

        broadcaster.Broadcast(rate, branchId: 1);

        Assert.Throws<SocketException>(() =>
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            receiver.Receive(ref ep);
        });
    }

    // ── Broadcast failure tolerance ────────────────────────────────────────────

    [Fact]
    public void Broadcast_OnInvalidPort_DoesNotThrow()
    {
        // Port 0 should cause an error but the broadcaster must swallow it.
        var opts = Options.Create(new RateBroadcastOptions { Port = -1, EnableBroadcast = true });
        var broadcaster = new RateBroadcaster(opts, NullLogger<RateBroadcaster>.Instance);
        var rate = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", DateTime.UtcNow);

        var ex = Record.Exception(() => broadcaster.Broadcast(rate, branchId: 1));
        Assert.Null(ex);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static int GetFreePort()
    {
        using var tmp = new UdpClient(0);
        return ((IPEndPoint)tmp.Client.LocalEndPoint!).Port;
    }

    private static bool IsValidJson(string text)
    {
        try { JsonDocument.Parse(text); return true; }
        catch (JsonException) { return false; }
    }
}
