using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.ObjectModel;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for the Backup & Restore settings tab.
/// Supports manual backup, restore, auto-backup toggle, and recent backups list.
/// </summary>
public sealed partial class BackupSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IBackupService   _backupService;

    [ObservableProperty] private string   _backupLocation      = string.Empty;
    [ObservableProperty] private bool     _autoBackupEnabled   = true;
    [ObservableProperty] private int      _backupIntervalHours = 24;
    [ObservableProperty] private DateTime _lastBackupTime      = DateTime.MinValue;
    [ObservableProperty] private int      _maxBackupsToKeep    = 5;
    [ObservableProperty] private bool     _backupInProgress;
    [ObservableProperty] private bool     _restoreInProgress;
    [ObservableProperty] private string   _statusMessage       = string.Empty;
    [ObservableProperty] private bool     _hasError;

    public ObservableCollection<BackupMetadata> RecentBackups { get; } = new();

    public string LastBackupDisplay =>
        LastBackupTime == DateTime.MinValue
            ? "Never"
            : LastBackupTime.ToString("dd-MMM-yyyy HH:mm");

    public BackupSettingsViewModel(ISettingsService settingsService, IBackupService backupService)
    {
        _settingsService = settingsService;
        _backupService   = backupService;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        var s = await _settingsService.LoadBackupSettingsAsync();
        BackupLocation      = s.BackupLocation;
        AutoBackupEnabled   = s.AutoBackupEnabled;
        BackupIntervalHours = s.BackupIntervalHours;
        LastBackupTime      = s.LastBackupTime;
        MaxBackupsToKeep    = s.MaxBackupsToKeep;
        OnPropertyChanged(nameof(LastBackupDisplay));
        await RefreshRecentBackupsAsync();
    }

    [RelayCommand]
    public async Task BackupNowAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupLocation))
        {
            StatusMessage = "Please set a backup location first.";
            HasError = true;
            return;
        }

        BackupInProgress = true;
        HasError = false;
        try
        {
            var path = await _backupService.BackupDatabaseAsync(BackupLocation, "Manual backup");
            LastBackupTime = DateTime.Now;
            OnPropertyChanged(nameof(LastBackupDisplay));

            var settings = await _settingsService.LoadBackupSettingsAsync();
            settings.LastBackupTime = LastBackupTime;
            await _settingsService.SaveBackupSettingsAsync(settings);

            await _backupService.DeleteOldBackupsAsync(BackupLocation, MaxBackupsToKeep);
            await RefreshRecentBackupsAsync();
            StatusMessage = $"Backup created: {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
            HasError = true;
        }
        finally
        {
            BackupInProgress = false;
        }
    }

    [RelayCommand]
    public async Task RestoreAsync(string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            StatusMessage = "No backup file selected.";
            HasError = true;
            return;
        }

        RestoreInProgress = true;
        HasError = false;
        try
        {
            await _backupService.RestoreDatabaseAsync(backupFilePath);
            StatusMessage = "Database restored successfully. Please restart the application.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
            HasError = true;
        }
        finally
        {
            RestoreInProgress = false;
        }
    }

    [RelayCommand]
    public async Task DeleteOldBackupsAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupLocation)) return;
        await _backupService.DeleteOldBackupsAsync(BackupLocation, MaxBackupsToKeep);
        await RefreshRecentBackupsAsync();
        StatusMessage = "Old backups cleaned up.";
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        var s = new BackupSettings
        {
            BackupLocation      = BackupLocation,
            AutoBackupEnabled   = AutoBackupEnabled,
            BackupIntervalHours = BackupIntervalHours,
            LastBackupTime      = LastBackupTime,
            MaxBackupsToKeep    = MaxBackupsToKeep
        };
        await _settingsService.SaveBackupSettingsAsync(s);
        StatusMessage = "Backup settings saved.";
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task RefreshRecentBackupsAsync()
    {
        RecentBackups.Clear();
        if (string.IsNullOrWhiteSpace(BackupLocation)) return;
        var list = await _backupService.GetRecentBackupsAsync(BackupLocation, MaxBackupsToKeep);
        foreach (var item in list)
            RecentBackups.Add(item);
    }
}
