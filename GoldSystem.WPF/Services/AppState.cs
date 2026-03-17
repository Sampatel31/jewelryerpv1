using CommunityToolkit.Mvvm.ComponentModel;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Application-wide singleton state shared across all ViewModels.
/// Holds current branch, user context, and connectivity status.
/// </summary>
public sealed class AppState : ObservableObject
{
    private int _currentBranchId = 1;
    private string _currentBranchName = "Default Branch";
    private int _currentUserId;
    private string _currentUserName = "Admin";
    private bool _isOwnerBranch = true;
    private bool _isOnline = true;
    private int _pendingSyncCount;
    private decimal _currentRate24K;
    private decimal _currentRate22K;
    private decimal _currentRate18K;
    private DateTime _rateUpdatedAt = DateTime.MinValue;
    private string _rateSource = string.Empty;

    public int CurrentBranchId
    {
        get => _currentBranchId;
        set => SetProperty(ref _currentBranchId, value);
    }

    public string CurrentBranchName
    {
        get => _currentBranchName;
        set => SetProperty(ref _currentBranchName, value);
    }

    public int CurrentUserId
    {
        get => _currentUserId;
        set => SetProperty(ref _currentUserId, value);
    }

    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    public bool IsOwnerBranch
    {
        get => _isOwnerBranch;
        set => SetProperty(ref _isOwnerBranch, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }

    public int PendingSyncCount
    {
        get => _pendingSyncCount;
        set => SetProperty(ref _pendingSyncCount, value);
    }

    public decimal CurrentRate24K
    {
        get => _currentRate24K;
        set => SetProperty(ref _currentRate24K, value);
    }

    public decimal CurrentRate22K
    {
        get => _currentRate22K;
        set => SetProperty(ref _currentRate22K, value);
    }

    public decimal CurrentRate18K
    {
        get => _currentRate18K;
        set => SetProperty(ref _currentRate18K, value);
    }

    public DateTime RateUpdatedAt
    {
        get => _rateUpdatedAt;
        set => SetProperty(ref _rateUpdatedAt, value);
    }

    public string RateSource
    {
        get => _rateSource;
        set => SetProperty(ref _rateSource, value);
    }

    /// <summary>Formatted 24K rate display string.</summary>
    public string Rate24KDisplay => _currentRate24K > 0
        ? $"24K: ₹{_currentRate24K:N0}/10g"
        : "24K: –";

    /// <summary>Formatted 22K rate display string.</summary>
    public string Rate22KDisplay => _currentRate22K > 0
        ? $"22K: ₹{_currentRate22K:N0}/10g"
        : "22K: –";

    /// <summary>Formatted 18K rate display string.</summary>
    public string Rate18KDisplay => _currentRate18K > 0
        ? $"18K: ₹{_currentRate18K:N0}/10g"
        : "18K: –";

    /// <summary>Updates all gold rate fields atomically.</summary>
    public void UpdateRates(decimal rate24K, decimal rate22K, decimal rate18K, string source)
    {
        CurrentRate24K = rate24K;
        CurrentRate22K = rate22K;
        CurrentRate18K = rate18K;
        RateSource = source;
        RateUpdatedAt = DateTime.Now;
        OnPropertyChanged(nameof(Rate24KDisplay));
        OnPropertyChanged(nameof(Rate22KDisplay));
        OnPropertyChanged(nameof(Rate18KDisplay));
    }
}
