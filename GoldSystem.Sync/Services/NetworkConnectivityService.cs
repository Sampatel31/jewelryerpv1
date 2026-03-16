using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace GoldSystem.Sync.Services;

/// <summary>
/// Tests whether the owner PC's SQL Server is reachable over the LAN.
/// </summary>
public interface INetworkConnectivityService
{
    Task<bool> CanReachOwnerDbAsync(string ownerIpAddress, int timeout = 5000);
}

/// <summary>
/// Probes TCP port 1433 (SQL Server default) to determine LAN reachability.
/// </summary>
public class NetworkConnectivityService : INetworkConnectivityService
{
    private readonly ILogger<NetworkConnectivityService> _logger;

    public NetworkConnectivityService(ILogger<NetworkConnectivityService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tries to establish a TCP connection to the owner SQL Server on port 1433.
    /// Returns <c>false</c> if the connection cannot be established within
    /// <paramref name="timeout"/> milliseconds.
    /// </summary>
    public async Task<bool> CanReachOwnerDbAsync(string ownerIpAddress, int timeout = 5000)
    {
        if (string.IsNullOrWhiteSpace(ownerIpAddress))
        {
            _logger.LogWarning("Owner IP address is null or empty");
            return false;
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ownerIpAddress, 1433);
            var timeoutTask = Task.Delay(timeout);

            var completed = await Task.WhenAny(connectTask, timeoutTask);

            if (completed == timeoutTask)
            {
                _logger.LogWarning("Connection timeout to {IP}:1433", ownerIpAddress);
                return false;
            }

            // Propagate any connection exception.
            await connectTask;

            _logger.LogInformation("Successfully connected to owner DB at {IP}", ownerIpAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot reach owner DB at {IP}", ownerIpAddress);
            return false;
        }
    }
}
