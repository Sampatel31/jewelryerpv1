using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for the Tax Settings tab.
/// Validates that all rates are between 0 and 100 percent.
/// </summary>
public sealed partial class TaxSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private decimal _cGSTRate              = 9m;
    [ObservableProperty] private decimal _sGSTRate              = 9m;
    [ObservableProperty] private decimal _iGSTRate              = 5m;
    [ObservableProperty] private decimal _exemptThreshold       = 0m;
    [ObservableProperty] private decimal _defaultMakingPercent  = 12m;
    [ObservableProperty] private decimal _defaultWastagePercent = 2m;
    [ObservableProperty] private decimal _defaultStoneCharge    = 0m;
    [ObservableProperty] private string  _hSNCode               = "7108";
    [ObservableProperty] private bool    _applyToAllBills       = true;
    [ObservableProperty] private bool    _isSaving;
    [ObservableProperty] private string  _statusMessage         = string.Empty;
    [ObservableProperty] private bool    _hasError;

    public TaxSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>Returns true when the given rate is in [0, 100].</summary>
    public static bool IsRateValid(decimal rate) => rate >= 0m && rate <= 100m;

    public bool AllRatesValid =>
        IsRateValid(CGSTRate) && IsRateValid(SGSTRate) && IsRateValid(IGSTRate)
        && IsRateValid(DefaultMakingPercent) && IsRateValid(DefaultWastagePercent);

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        var s = await _settingsService.LoadTaxSettingsAsync();
        CGSTRate              = s.CGSTRate;
        SGSTRate              = s.SGSTRate;
        IGSTRate              = s.IGSTRate;
        ExemptThreshold       = s.ExemptThreshold;
        DefaultMakingPercent  = s.DefaultMakingPercent;
        DefaultWastagePercent = s.DefaultWastagePercent;
        DefaultStoneCharge    = s.DefaultStoneCharge;
        HSNCode               = s.HSNCode;
        ApplyToAllBills       = s.ApplyToAllBills;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!AllRatesValid)
        {
            StatusMessage = "All tax rates must be between 0 and 100.";
            HasError = true;
            return;
        }

        IsSaving = true;
        HasError = false;
        try
        {
            var s = new TaxSettings
            {
                CGSTRate              = CGSTRate,
                SGSTRate              = SGSTRate,
                IGSTRate              = IGSTRate,
                ExemptThreshold       = ExemptThreshold,
                DefaultMakingPercent  = DefaultMakingPercent,
                DefaultWastagePercent = DefaultWastagePercent,
                DefaultStoneCharge    = DefaultStoneCharge,
                HSNCode               = HSNCode,
                ApplyToAllBills       = ApplyToAllBills
            };
            await _settingsService.SaveTaxSettingsAsync(s);
            StatusMessage = "Tax settings saved successfully.";
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
        var d = new TaxSettings();
        CGSTRate              = d.CGSTRate;
        SGSTRate              = d.SGSTRate;
        IGSTRate              = d.IGSTRate;
        ExemptThreshold       = d.ExemptThreshold;
        DefaultMakingPercent  = d.DefaultMakingPercent;
        DefaultWastagePercent = d.DefaultWastagePercent;
        DefaultStoneCharge    = d.DefaultStoneCharge;
        HSNCode               = d.HSNCode;
        ApplyToAllBills       = d.ApplyToAllBills;
        StatusMessage = string.Empty;
        await Task.CompletedTask;
    }
}
