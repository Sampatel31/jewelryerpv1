using GoldSystem.RateEngine.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Broadcasts the current gold rate to all PCs on the LAN via UDP.
/// Only runs on the owner-branch PC (<c>IsOwnerBranch = true</c>).
/// </summary>
public class RateBroadcaster
{
    private readonly IOptions<RateBroadcastOptions> _options;
    private readonly ILogger<RateBroadcaster> _logger;

    public RateBroadcaster(IOptions<RateBroadcastOptions> options, ILogger<RateBroadcaster> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Serialises <paramref name="rate"/> + <paramref name="branchId"/> as JSON and
    /// UDP-broadcasts the packet to 255.255.255.255 on the configured port.
    /// A broadcast failure is logged as a warning but never propagates.
    /// </summary>
    public void Broadcast(GoldRateResult rate, int branchId)
    {
        if (!_options.Value.EnableBroadcast) return;

        try
        {
            var payload = new BroadcastPayload(rate, branchId);
            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            var port = _options.Value.Port;

            using var udp = new UdpClient { EnableBroadcast = true };
            udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, port));

            _logger.LogInformation(
                "Rate broadcast sent to 255.255.255.255:{Port} for branch {BranchId}: 24K={Rate24K}",
                port, branchId, rate.Rate24K);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP rate broadcast failed – bill creation is NOT blocked.");
        }
    }
}

/// <summary>Wire format of the UDP broadcast message.</summary>
internal record BroadcastPayload(GoldRateResult Rate, int BranchId);
