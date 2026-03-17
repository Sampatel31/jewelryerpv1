namespace GoldSystem.Core.Interfaces;

// ─── IErrorHandlingService ────────────────────────────────────────────────────

/// <summary>Centralised error handling: dialogs, toasts, and exception routing.</summary>
public interface IErrorHandlingService
{
    /// <summary>Logs and translates an exception into a user-facing dialog.</summary>
    Task HandleExceptionAsync(Exception exception);

    /// <summary>Shows a modal error dialog.</summary>
    void ShowErrorDialog(string message, string title = "Error");

    /// <summary>Shows a short-lived success notification.</summary>
    void ShowSuccessToast(string message, int durationMs = 3000);

    /// <summary>Shows a short-lived warning notification.</summary>
    void ShowWarningToast(string message, int durationMs = 3000);

    /// <summary>Shows a short-lived informational notification.</summary>
    void ShowInfoToast(string message, int durationMs = 3000);

    /// <summary>
    /// Evaluates <paramref name="validationRule"/>; throws
    /// <see cref="GoldSystem.Core.Models.ValidationException"/> when it returns <c>false</c>.
    /// </summary>
    void ValidateInput(string fieldName, string fieldValue,
        Func<bool> validationRule, string errorMessage);
}

// ─── ILoggingService ──────────────────────────────────────────────────────────

/// <summary>Structured application logger with domain-specific helpers.</summary>
public interface ILoggingService
{
    void LogDebug(string message, params object[] args);
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogFatal(Exception exception, string message, params object[] args);

    /// <summary>Logs operation duration; emits a warning when over 1 000 ms.</summary>
    void LogPerformance(string operation, long durationMs);

    /// <summary>Records a user action for the audit trail.</summary>
    void LogUserAction(string userId, string action, string module, string entity);

    /// <summary>Records an outbound API call with status and latency.</summary>
    void LogApiCall(string method, string endpoint, int statusCode, long durationMs);
}

// ─── ICachingService ──────────────────────────────────────────────────────────

/// <summary>In-process memory cache with TTL support.</summary>
public interface ICachingService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task RemoveAsync(string key);
    Task ClearAsync();

    /// <summary>Returns a cached value or creates and caches it via <paramref name="factory"/>.</summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
}

// ─── IPerformanceService ──────────────────────────────────────────────────────

/// <summary>Measures operation durations and exposes aggregated metrics.</summary>
public interface IPerformanceService
{
    Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action);
    Task MeasureAsync(string operation, Func<Task> action);
    GoldSystem.Core.Models.PerformanceMetrics GetMetrics();
}
