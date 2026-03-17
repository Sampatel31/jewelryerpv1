using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class SyncStatusView : UserControl
{
    public SyncStatusView(SyncStatusViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
