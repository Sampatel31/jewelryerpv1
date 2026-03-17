using CommunityToolkit.Mvvm.ComponentModel;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

public sealed partial class AboutViewModel : BaseViewModel
{
    public string AppVersion => "1.0.0";
    public string AppTitle => "Gold Jewellery Management System";
    public string AppDescription => "Complete ERP for gold jewellery shops with billing, inventory, sync, and AI insights.";

    public AboutViewModel(NavigationService navigation, AppState appState)
        : base(navigation, appState) { }
}
