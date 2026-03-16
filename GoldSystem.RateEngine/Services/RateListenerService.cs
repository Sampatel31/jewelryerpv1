using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.RateEngine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Background service that runs on branch (non-owner) PCs.
/// Listens on the UDP broadcast port and updates the local rate cache and UI
/// whenever the owner PC sends a new gold rate.
/// </summary>
public sealed class RateListenerService : BackgroundService
{
    private readonly IOptions<RateBroadcastOptions> _options;
    private readonly RateChangedEventPublisher _publisher;
    private readonly IDbContextFactory<GoldDbContext> _dbFactory;
    private readonly ILogger<RateListenerService> _logger;

    public RateListenerService(
        IOptions<RateBroadcastOptions> options,
        RateChangedEventPublisher publisher,
        IDbContextFactory<GoldDbContext> dbFactory,
        ILogger<RateListenerService> logger)
    {
        _options = options;
        _publisher = publisher;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = _options.Value.Port;
        _logger.LogInformation("RateListenerService starting on UDP port {Port}", port);

        using var udp = new UdpClient(port) { EnableBroadcast = true };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(stoppingToken);
                var json = Encoding.UTF8.GetString(result.Buffer);

                await HandleMessageAsync(json, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in RateListenerService receive loop; continuing.");
            }
        }

        _logger.LogInformation("RateListenerService stopped.");
    }

    private async Task HandleMessageAsync(string json, CancellationToken ct)
    {
        BroadcastPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BroadcastPayload>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed broadcast message received; ignoring.");
            return;
        }

        if (payload?.Rate is null)
        {
            _logger.LogWarning("Broadcast payload contained null rate; ignoring.");
            return;
        }

        _logger.LogInformation(
            "Received broadcast rate from branch {BranchId}: 24K={Rate24K}",
            payload.BranchId, payload.Rate.Rate24K);

        // Persist to SyncQueue so the sync subsystem knows this record is already in sync.
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.SyncQueues.Add(new SyncQueue
        {
            TableName = "GoldRates",
            RecordId = 0,
            Operation = "INSERT",
            Payload = json,
            BranchId = payload.BranchId,
            CreatedAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            Status = "Synced"
        });
        await db.SaveChangesAsync(ct);

        // Notify all UI subscribers immediately.
        _publisher.Publish(new RateChangeEvent
        {
            Rate = payload.Rate,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = "LAN_BROADCAST"
        });
    }
}
