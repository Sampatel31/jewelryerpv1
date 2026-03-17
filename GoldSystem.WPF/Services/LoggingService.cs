using GoldSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Structured logger with domain-specific helpers for performance, user
/// actions, and API call tracking.
/// </summary>
public sealed class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    public void LogDebug(string message, params object[] args)
        => _logger.LogDebug(message, args);

    public void LogInfo(string message, params object[] args)
        => _logger.LogInformation(message, args);

    public void LogWarning(string message, params object[] args)
        => _logger.LogWarning(message, args);

    public void LogError(Exception exception, string message, params object[] args)
        => _logger.LogError(exception, message, args);

    public void LogFatal(Exception exception, string message, params object[] args)
        => _logger.LogCritical(exception, message, args);

    public void LogPerformance(string operation, long durationMs)
    {
        if (durationMs > 1_000)
            _logger.LogWarning("Slow operation: {Operation} took {Duration}ms", operation, durationMs);
        else
            _logger.LogInformation("Operation: {Operation} completed in {Duration}ms", operation, durationMs);
    }

    public void LogUserAction(string userId, string action, string module, string entity)
        => _logger.LogInformation(
            "User {UserId} performed {Action} on {Module}.{Entity}",
            userId, action, module, entity);

    public void LogApiCall(string method, string endpoint, int statusCode, long durationMs)
    {
        var level = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        _logger.Log(level,
            "{Method} {Endpoint} – Status {StatusCode} – {Duration}ms",
            method, endpoint, statusCode, durationMs);
    }
}
