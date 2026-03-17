using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Text.RegularExpressions;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for the Company Settings tab.
/// Validates GSTIN (15-char alphanumeric) and email format before saving.
/// </summary>
public sealed partial class CompanySettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    // ── Observable Properties ─────────────────────────────────────────────────
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _gSTIN       = string.Empty;
    [ObservableProperty] private string _email       = string.Empty;
    [ObservableProperty] private string _phone       = string.Empty;
    [ObservableProperty] private string _address     = string.Empty;
    [ObservableProperty] private string _state       = string.Empty;
    [ObservableProperty] private string _city        = string.Empty;
    [ObservableProperty] private string _postalCode  = string.Empty;
    [ObservableProperty] private string _bankName    = string.Empty;
    [ObservableProperty] private string _accountNo   = string.Empty;
    [ObservableProperty] private string _iFSC        = string.Empty;
    [ObservableProperty] private string _logoPath    = string.Empty;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _hasError;
    [ObservableProperty] private string _gstinValidationMessage = string.Empty;
    [ObservableProperty] private string _emailValidationMessage = string.Empty;

    // ── Static validation regexes ─────────────────────────────────────────────
    private static readonly Regex GstinRegex =
        new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CompanySettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>Returns true if GSTIN is empty or matches the 15-character GST format.</summary>
    public bool IsGstinValid(string gstin) =>
        string.IsNullOrWhiteSpace(gstin) || GstinRegex.IsMatch(gstin);

    /// <summary>Returns true if email is empty or is a valid email address.</summary>
    public bool IsEmailValid(string email) =>
        string.IsNullOrWhiteSpace(email) || EmailRegex.IsMatch(email);

    partial void OnGSTINChanged(string value)
    {
        GstinValidationMessage = IsGstinValid(value) ? string.Empty
            : "GSTIN must be 15 alphanumeric characters (e.g. 27AAPFU0939F1ZV).";
    }

    partial void OnEmailChanged(string value)
    {
        EmailValidationMessage = IsEmailValid(value) ? string.Empty
            : "Please enter a valid email address.";
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        var s = await _settingsService.LoadCompanySettingsAsync();
        CompanyName = s.CompanyName;
        GSTIN       = s.GSTIN;
        Email       = s.Email;
        Phone       = s.Phone;
        Address     = s.Address;
        State       = s.State;
        City        = s.City;
        PostalCode  = s.PostalCode;
        BankName    = s.BankName;
        AccountNo   = s.AccountNo;
        IFSC        = s.IFSC;
        LogoPath    = s.LogoPath;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!IsGstinValid(GSTIN))
        {
            StatusMessage = "Please fix GSTIN before saving.";
            HasError = true;
            return;
        }
        if (!IsEmailValid(Email))
        {
            StatusMessage = "Please fix email before saving.";
            HasError = true;
            return;
        }

        IsSaving = true;
        HasError = false;
        try
        {
            var s = new CompanySettings
            {
                CompanyName = CompanyName,
                GSTIN       = GSTIN,
                Email       = Email,
                Phone       = Phone,
                Address     = Address,
                State       = State,
                City        = City,
                PostalCode  = PostalCode,
                BankName    = BankName,
                AccountNo   = AccountNo,
                IFSC        = IFSC,
                LogoPath    = LogoPath
            };
            await _settingsService.SaveCompanySettingsAsync(s);
            StatusMessage = "Company settings saved successfully.";
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
    public async Task ResetAsync()
    {
        var defaults = new CompanySettings();
        CompanyName = defaults.CompanyName;
        GSTIN       = defaults.GSTIN;
        Email       = defaults.Email;
        Phone       = defaults.Phone;
        Address     = defaults.Address;
        State       = defaults.State;
        City        = defaults.City;
        PostalCode  = defaults.PostalCode;
        BankName    = defaults.BankName;
        AccountNo   = defaults.AccountNo;
        IFSC        = defaults.IFSC;
        LogoPath    = defaults.LogoPath;
        StatusMessage = string.Empty;
        await Task.CompletedTask;
    }
}
