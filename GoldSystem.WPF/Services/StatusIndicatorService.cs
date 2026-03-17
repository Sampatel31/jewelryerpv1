using CommunityToolkit.Mvvm.ComponentModel;
using GoldSystem.RateEngine.Models;
using GoldSystem.RateEngine.Services;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Provides real-time gold rate and sync-status indicators to all ViewModels.
/// Subscribes to <see cref="RateChangedEventPublisher"/> and exposes observable properties.
/// </summary>
public sealed class StatusIndicatorService : ObservableObject, IDisposable
{
    private readonly AppState _appState;
    private readonly RateChangedEventPublisher _ratePublisher;
    private readonly ILogger<StatusIndicatorService> _logger;

    private string _syncStatus = "Idle";
    private string _syncStatusIcon = "CheckCircle";
    private bool _isSyncing;

    public StatusIndicatorService(
        AppState appState,
        RateChangedEventPublisher ratePublisher,
        ILogger<StatusIndicatorService> logger)
    {
        _appState = appState;
        _ratePublisher = ratePublisher;
        _logger = logger;
        _ratePublisher.OnRateChanged += HandleRateChanged;
    }

    /// <summary>Sync status text shown in the status bar.</summary>
    public string SyncStatus
    {
        get => _syncStatus;
        private set => SetProperty(ref _syncStatus, value);
    }

    /// <summary>Material Design icon name for the current sync state.</summary>
    public string SyncStatusIcon
    {
        get => _syncStatusIcon;
        private set => SetProperty(ref _syncStatusIcon, value);
    }

    /// <summary>True while a sync operation is in progress.</summary>
    public bool IsSyncing
    {
        get => _isSyncing;
        private set => SetProperty(ref _isSyncing, value);
    }

    /// <summary>Called when a sync cycle starts.</summary>
    public void ReportSyncStarted()
    {
        IsSyncing = true;
        SyncStatus = "Syncing…";
        SyncStatusIcon = "Sync";
    }

    /// <summary>Called when a sync cycle completes successfully.</summary>
    public void ReportSyncCompleted(int recordsSynced)
    {
        IsSyncing = false;
        SyncStatus = recordsSynced > 0
            ? $"Synced {recordsSynced} record(s)"
            : "Up to date";
        SyncStatusIcon = "CheckCircle";
        _appState.PendingSyncCount = 0;
    }

    /// <summary>Called when a sync cycle fails.</summary>
    public void ReportSyncFailed(string reason)
    {
        IsSyncing = false;
        SyncStatus = $"Sync failed: {reason}";
        SyncStatusIcon = "AlertCircle";
        _logger.LogWarning("Sync failed: {Reason}", reason);
    }

    /// <summary>Called when the owner PC is unreachable.</summary>
    public void ReportOffline(int pendingCount)
    {
        IsSyncing = false;
        SyncStatus = $"Offline – {pendingCount} pending";
        SyncStatusIcon = "CloudOff";
        _appState.PendingSyncCount = pendingCount;
        _appState.IsOnline = false;
    }

    private void HandleRateChanged(object? sender, RateChangeEvent e)
    {
        _appState.UpdateRates(
            e.Rate.Rate24K,
            e.Rate.Rate22K,
            e.Rate.Rate18K,
            e.Rate.Source);
        _logger.LogDebug("Rate updated: 24K={Rate24K}", e.Rate.Rate24K);
    }

    public void Dispose()
    {
        _ratePublisher.OnRateChanged -= HandleRateChanged;
    }
}
