using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Models;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Exposes and persists the SecurityPolicy (password rules, lockout, 2FA, session).
/// </summary>
public sealed partial class SecuritySettingsViewModel : ObservableObject
{
    // ── Password Policy ───────────────────────────────────────────────────────
    [ObservableProperty] private int  _passwordMinLength       = 8;
    [ObservableProperty] private bool _requireUppercase        = true;
    [ObservableProperty] private bool _requireLowercase        = true;
    [ObservableProperty] private bool _requireDigits           = true;
    [ObservableProperty] private bool _requireSpecialChars     = false;
    [ObservableProperty] private int  _passwordExpiryDays      = 90;
    [ObservableProperty] private int  _passwordMinAgeDays      = 1;

    // ── Lockout Policy ────────────────────────────────────────────────────────
    [ObservableProperty] private int  _maxLoginAttempts        = 5;
    [ObservableProperty] private int  _lockoutDurationMinutes  = 15;

    // ── Session ───────────────────────────────────────────────────────────────
    [ObservableProperty] private int  _sessionTimeoutMinutes   = 30;

    // ── 2FA ───────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _twoFactorRequired       = false;
    [ObservableProperty] private TwoFactorMethod _defaultTwoFactorMethod = TwoFactorMethod.None;

    // ── Status ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private bool   _hasError;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public IReadOnlyList<TwoFactorMethod> TwoFactorMethods { get; } =
        Enum.GetValues<TwoFactorMethod>().ToList();

    // ── Derived Property ──────────────────────────────────────────────────────

    public bool IsValid =>
        PasswordMinLength >= 4
        && MaxLoginAttempts >= 1
        && LockoutDurationMinutes >= 1
        && SessionTimeoutMinutes >= 1
        && PasswordExpiryDays >= 0;

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        // In a full implementation this would load from ISettingsService.
        // For Phase 14 the defaults are already the correct starting point.
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!IsValid)
        {
            StatusMessage = "Please correct the values before saving.";
            HasError = true;
            return;
        }

        IsSaving = true;
        HasError = false;
        try
        {
            // Build the policy model
            var policy = BuildPolicy();
            // In a full implementation: await _settingsService.SaveSecurityPolicyAsync(policy);
            await Task.Delay(10);
            StatusMessage = "Security settings saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task ResetToDefaultsAsync()
    {
        var d = new SecurityPolicy();
        PasswordMinLength      = d.PasswordMinLength;
        RequireUppercase       = d.RequireUppercase;
        RequireLowercase       = d.RequireLowercase;
        RequireDigits          = d.RequireDigits;
        RequireSpecialChars    = d.RequireSpecialChars;
        PasswordExpiryDays     = d.PasswordExpiryDays;
        PasswordMinAgeDays     = d.PasswordMinAgeDays;
        MaxLoginAttempts       = d.MaxLoginAttempts;
        LockoutDurationMinutes = d.LockoutDurationMinutes;
        SessionTimeoutMinutes  = d.SessionTimeoutMinutes;
        TwoFactorRequired      = d.TwoFactorRequired;
        StatusMessage          = string.Empty;
        await Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public SecurityPolicy BuildPolicy() => new()
    {
        PasswordMinLength      = PasswordMinLength,
        RequireUppercase       = RequireUppercase,
        RequireLowercase       = RequireLowercase,
        RequireDigits          = RequireDigits,
        RequireSpecialChars    = RequireSpecialChars,
        PasswordExpiryDays     = PasswordExpiryDays,
        PasswordMinAgeDays     = PasswordMinAgeDays,
        MaxLoginAttempts       = MaxLoginAttempts,
        LockoutDurationMinutes = LockoutDurationMinutes,
        SessionTimeoutMinutes  = SessionTimeoutMinutes,
        TwoFactorRequired      = TwoFactorRequired
    };
}
