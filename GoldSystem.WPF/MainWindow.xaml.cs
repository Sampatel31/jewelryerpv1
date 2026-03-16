using GoldSystem.WPF.Services;
using System.Windows;

namespace GoldSystem.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;

    public MainWindow(NavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _navigationService.Initialize(MainFrame);
    }
}