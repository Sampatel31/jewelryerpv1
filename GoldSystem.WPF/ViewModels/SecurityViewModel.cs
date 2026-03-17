using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Main coordinator for the 4-tab Security dashboard.
/// Owns child ViewModels for Users, RBAC, Audit Trail, and Settings.
/// </summary>
public sealed partial class SecurityViewModel : BaseViewModel
{
    // ── Child ViewModels ──────────────────────────────────────────────────────
    public UserManagementViewModel UserManagement   { get; }
    public RBACViewModel           RBAC             { get; }
    public AuditTrailViewModel     AuditTrail       { get; }
    public SecuritySettingsViewModel SecuritySettings { get; }

    // ── Tab Index ─────────────────────────────────────────────────────────────
    [ObservableProperty] private int _selectedTabIndex;

    public SecurityViewModel(
        NavigationService          navigation,
        AppState                   appState,
        UserManagementViewModel    userManagement,
        RBACViewModel              rbac,
        AuditTrailViewModel        auditTrail,
        SecuritySettingsViewModel  securitySettings)
        : base(navigation, appState)
    {
        UserManagement   = userManagement;
        RBAC             = rbac;
        AuditTrail       = auditTrail;
        SecuritySettings = securitySettings;
    }

    public override async Task OnNavigatedToAsync()
    {
        await UserManagement.LoadCommand.ExecuteAsync(null);
        await RBAC.LoadCommand.ExecuteAsync(null);
        await AuditTrail.LoadLogsCommand.ExecuteAsync(null);
        await SecuritySettings.LoadCommand.ExecuteAsync(null);
    }
}
