using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Centralised error handler: maps exceptions to user-friendly messages,
/// shows dialogs/toasts, and writes to the structured log.
/// </summary>
public sealed class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
    {
        _logger = logger;
    }

    // ── Exception routing ─────────────────────────────────────────────────────

    public async Task HandleExceptionAsync(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var userMessage = exception switch
        {
            ValidationException    => exception.Message,
            BusinessLogicException => exception.Message,
            DataException          => "A database error occurred. Please try again.",
            TimeoutException       => "The operation timed out. Please try again.",
            InvalidOperationException => "Invalid operation. Please check your input.",
            _                      => "An unexpected error occurred. Please contact support."
        };

        ShowErrorDialog(userMessage, "Error");
        await Task.CompletedTask;
    }

    // ── Dialogs & toasts ──────────────────────────────────────────────────────

    public void ShowErrorDialog(string message, string title = "Error")
    {
        _logger.LogWarning("Error dialog shown: {Title} – {Message}", title, message);
        // WPF MessageBox would be invoked here in a real UI context.
    }

    public void ShowSuccessToast(string message, int durationMs = 3000)
        => _logger.LogInformation("Success [{Duration}ms]: {Message}", durationMs, message);

    public void ShowWarningToast(string message, int durationMs = 3000)
        => _logger.LogWarning("Warning [{Duration}ms]: {Message}", durationMs, message);

    public void ShowInfoToast(string message, int durationMs = 3000)
        => _logger.LogInformation("Info [{Duration}ms]: {Message}", durationMs, message);

    // ── Validation helper ─────────────────────────────────────────────────────

    public void ValidateInput(string fieldName, string fieldValue,
        Func<bool> validationRule, string errorMessage)
    {
        if (!validationRule())
            throw new ValidationException($"{fieldName}: {errorMessage}");
    }
}
