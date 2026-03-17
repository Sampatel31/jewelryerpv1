using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
