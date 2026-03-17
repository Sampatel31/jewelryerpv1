using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Manages user accounts: view, add, edit, delete, reset password, toggle 2FA.
/// </summary>
public sealed partial class UserManagementViewModel : ObservableObject
{
    private readonly IRBACService      _rbacService;
    private readonly IPasswordService  _passwordService;
    private readonly IAuditService     _auditService;
    private readonly SecurityPolicy    _policy;

    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── Collections ──────────────────────────────────────────────────────────
    public ObservableCollection<AppUser> Users { get; } = new();
    public ObservableCollection<AppRole> Roles  { get; } = new();

    // ── Selection ─────────────────────────────────────────────────────────────
    [ObservableProperty] private AppUser? _selectedUser;

    // ── Form Fields ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _formUsername     = string.Empty;
    [ObservableProperty] private string _formEmail        = string.Empty;
    [ObservableProperty] private string _formPassword     = string.Empty;
    [ObservableProperty] private int    _formRoleId;
    [ObservableProperty] private bool   _formTwoFactor;
    [ObservableProperty] private TwoFactorMethod _formTwoFactorMethod = TwoFactorMethod.None;

    // ── Status ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private bool   _hasError;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isEditing;

    public UserManagementViewModel(
        IRBACService     rbacService,
        IPasswordService passwordService,
        IAuditService    auditService,
        SecurityPolicy?  policy = null)
    {
        _rbacService     = rbacService;
        _passwordService = passwordService;
        _auditService    = auditService;
        _policy          = policy ?? new SecurityPolicy();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var users = await _rbacService.GetAllUsersAsync();
            var roles = await _rbacService.GetAllRolesAsync();

            Users.Clear();
            foreach (var u in users) Users.Add(u);

            Roles.Clear();
            foreach (var r in roles) Roles.Add(r);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void NewUser()
    {
        SelectedUser   = null;
        FormUsername   = string.Empty;
        FormEmail      = string.Empty;
        FormPassword   = string.Empty;
        FormRoleId     = Roles.FirstOrDefault()?.Id ?? 0;
        FormTwoFactor  = false;
        FormTwoFactorMethod = TwoFactorMethod.None;
        IsEditing      = true;
        HasError       = false;
        StatusMessage  = string.Empty;
    }

    [RelayCommand]
    public void EditUser(AppUser? user)
    {
        if (user is null) return;
        SelectedUser   = user;
        FormUsername   = user.Username;
        FormEmail      = user.Email;
        FormPassword   = string.Empty;
        FormRoleId     = user.RoleId;
        FormTwoFactor  = user.TwoFactorEnabled;
        FormTwoFactorMethod = user.TwoFactorMethod;
        IsEditing      = true;
        HasError       = false;
        StatusMessage  = string.Empty;
    }

    [RelayCommand]
    public async Task SaveUserAsync()
    {
        if (!ValidateForm()) return;

        IsSaving = true;
        HasError = false;
        try
        {
            if (SelectedUser is null)
            {
                // Create new user
                var hash = _passwordService.HashPassword(FormPassword);
                var user = await _rbacService.CreateUserAsync(FormUsername, FormEmail, hash, FormRoleId);
                user.TwoFactorEnabled = FormTwoFactor;
                user.TwoFactorMethod  = FormTwoFactorMethod;
                await _rbacService.UpdateUserAsync(user);

                Users.Add(user);
                await _auditService.LogActionAsync(0, "UserCreated", "Security", "User",
                    user.Id.ToString(), newValue: FormUsername);
                StatusMessage = $"User '{FormUsername}' created successfully.";
            }
            else
            {
                // Update existing
                var existing = SelectedUser;
                existing.Username         = FormUsername;
                existing.Email            = FormEmail;
                existing.RoleId           = FormRoleId;
                existing.TwoFactorEnabled = FormTwoFactor;
                existing.TwoFactorMethod  = FormTwoFactorMethod;

                if (!string.IsNullOrWhiteSpace(FormPassword))
                    existing.PasswordHash = _passwordService.HashPassword(FormPassword);

                await _rbacService.UpdateUserAsync(existing);
                await _auditService.LogActionAsync(0, "UserUpdated", "Security", "User",
                    existing.Id.ToString(), newValue: FormUsername);
                StatusMessage = $"User '{FormUsername}' updated successfully.";

                // Refresh list
                var idx = Users.IndexOf(Users.FirstOrDefault(u => u.Id == existing.Id)!);
                if (idx >= 0) { Users[idx] = existing; }
            }

            IsEditing = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task DeleteUserAsync(AppUser? user)
    {
        if (user is null) return;
        await _rbacService.DeleteUserAsync(user.Id);
        Users.Remove(user);
        await _auditService.LogActionAsync(0, "UserDeleted", "Security", "User",
            user.Id.ToString(), oldValue: user.Username);
        StatusMessage = $"User '{user.Username}' deleted.";
    }

    [RelayCommand]
    public async Task ResetPasswordAsync(AppUser? user)
    {
        if (user is null) return;

        // Generate a temporary password
        var tempPw = $"Temp@{_passwordService.GenerateOTP(4)}";
        user.PasswordHash = _passwordService.HashPassword(tempPw);
        await _rbacService.UpdateUserAsync(user);
        await _auditService.LogActionAsync(0, "PasswordReset", "Security", "User",
            user.Id.ToString());
        StatusMessage = $"Password reset for '{user.Username}'. Temp password: {tempPw}";
    }

    [RelayCommand]
    public async Task Toggle2FAAsync(AppUser? user)
    {
        if (user is null) return;
        user.TwoFactorEnabled = !user.TwoFactorEnabled;
        await _rbacService.UpdateUserAsync(user);
        StatusMessage = $"2FA {(user.TwoFactorEnabled ? "enabled" : "disabled")} for '{user.Username}'.";
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing     = false;
        StatusMessage = string.Empty;
        HasError      = false;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    public bool IsEmailValid(string email) =>
        string.IsNullOrWhiteSpace(email) || EmailRegex.IsMatch(email);

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(FormUsername))
        {
            StatusMessage = "Username is required.";
            HasError = true;
            return false;
        }

        if (!IsEmailValid(FormEmail))
        {
            StatusMessage = "Please enter a valid email address.";
            HasError = true;
            return false;
        }

        // New user must have a password
        if (SelectedUser is null && string.IsNullOrWhiteSpace(FormPassword))
        {
            StatusMessage = "Password is required for a new user.";
            HasError = true;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(FormPassword))
        {
            var (ok, msg) = _passwordService.ValidatePasswordPolicy(FormPassword, _policy);
            if (!ok) { StatusMessage = msg; HasError = true; return false; }
        }

        if (FormRoleId == 0)
        {
            StatusMessage = "Please select a role.";
            HasError = true;
            return false;
        }

        HasError = false;
        return true;
    }
}
