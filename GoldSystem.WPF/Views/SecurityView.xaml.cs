using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

/// <summary>Interaction logic for SecurityView.xaml</summary>
public partial class SecurityView : UserControl
{
    public SecurityView(SecurityViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
