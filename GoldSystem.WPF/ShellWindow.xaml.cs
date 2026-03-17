using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using GoldSystem.WPF.Views;
using System.Windows;

namespace GoldSystem.WPF;

/// <summary>
/// Shell window – initialises navigation and navigates to the Dashboard on startup.
/// </summary>
public partial class ShellWindow : Window
{
    private readonly NavigationService _navigation;
    private readonly ShellViewModel _viewModel;

    public ShellWindow(NavigationService navigation, ShellViewModel viewModel)
    {
        _navigation = navigation;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _navigation.Initialize(ContentArea);
        _viewModel.NavigateToDashboardCommand.Execute(null);
    }
}
