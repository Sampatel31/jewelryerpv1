using GoldSystem.RateEngine.Models;
using Microsoft.Extensions.Logging;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Thread-safe singleton that publishes <see cref="RateChangeEvent"/> to all subscribers.
/// ViewModels and other services subscribe via <see cref="OnRateChanged"/> to receive live updates.
/// </summary>
public sealed class RateChangedEventPublisher
{
    private readonly ILogger<RateChangedEventPublisher> _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private event EventHandler<RateChangeEvent>? _rateChanged;

    public RateChangedEventPublisher(ILogger<RateChangedEventPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>Subscribe to rate change notifications.</summary>
    public event EventHandler<RateChangeEvent> OnRateChanged
    {
        add
        {
            _lock.EnterWriteLock();
            try { _rateChanged += value; }
            finally { _lock.ExitWriteLock(); }
        }
        remove
        {
            _lock.EnterWriteLock();
            try { _rateChanged -= value; }
            finally { _lock.ExitWriteLock(); }
        }
    }

    /// <summary>Publish a new rate change to all subscribers.</summary>
    public void Publish(RateChangeEvent evt)
    {
        EventHandler<RateChangeEvent>? snapshot;
        _lock.EnterReadLock();
        try { snapshot = _rateChanged; }
        finally { _lock.ExitReadLock(); }

        if (snapshot is null) return;

        foreach (var handler in snapshot.GetInvocationList().Cast<EventHandler<RateChangeEvent>>())
        {
            try { handler(this, evt); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rate change handler threw an exception; continuing.");
            }
        }
    }
}
