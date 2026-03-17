using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.ObjectModel;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Displays a filterable, exportable audit trail grid.
/// </summary>
public sealed partial class AuditTrailViewModel : ObservableObject
{
    private readonly IAuditService _auditService;

    // ── Collections ──────────────────────────────────────────────────────────
    public ObservableCollection<AuditLog> AuditLogs { get; } = new();

    // ── Filters ───────────────────────────────────────────────────────────────
    [ObservableProperty] private int?     _filterUserId;
    [ObservableProperty] private string   _filterAction  = string.Empty;
    [ObservableProperty] private string   _filterModule  = string.Empty;
    [ObservableProperty] private DateTime _filterFrom    = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _filterTo      = DateTime.Today.AddDays(1);
    [ObservableProperty] private int      _maxRecords    = 500;

    // ── Status ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private bool   _hasError;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int    _totalCount;

    // ── Module / Action filter lists ──────────────────────────────────────────
    public IReadOnlyList<string> AvailableModules { get; } =
        new[] { string.Empty }
            .Concat(Enum.GetNames<PermissionModule>())
            .ToList();

    public IReadOnlyList<string> AvailableActions { get; } =
        new[] { string.Empty, "LoginSuccess", "LoginFailed", "AccountLocked",
                "UserCreated", "UserUpdated", "UserDeleted", "PasswordReset",
                "2FASuccess", "RolePermissionsUpdated" };

    public AuditTrailViewModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadLogsAsync()
    {
        IsLoading = true;
        HasError  = false;
        try
        {
            var logs = await _auditService.GetAuditLogsAsync(
                userId:     FilterUserId,
                action:     string.IsNullOrWhiteSpace(FilterAction) ? null : FilterAction,
                module:     string.IsNullOrWhiteSpace(FilterModule) ? null : FilterModule,
                from:       FilterFrom,
                to:         FilterTo,
                maxRecords: MaxRecords);

            AuditLogs.Clear();
            foreach (var l in logs) AuditLogs.Add(l);
            TotalCount    = AuditLogs.Count;
            StatusMessage = $"{TotalCount} log entries loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading logs: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExportExcelAsync()
    {
        if (!AuditLogs.Any())
        {
            StatusMessage = "No logs to export. Load logs first.";
            return;
        }

        try
        {
            var bytes = await _auditService.ExportAuditTrailAsync(AuditLogs.ToList());
            StatusMessage = $"Exported {AuditLogs.Count} log entries ({bytes.Length:N0} bytes).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
            HasError = true;
        }
    }

    [RelayCommand]
    public async Task FilterLogsAsync()
    {
        await LoadLogsAsync();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        FilterUserId = null;
        FilterAction = string.Empty;
        FilterModule = string.Empty;
        FilterFrom   = DateTime.Today.AddDays(-30);
        FilterTo     = DateTime.Today.AddDays(1);
    }
}
