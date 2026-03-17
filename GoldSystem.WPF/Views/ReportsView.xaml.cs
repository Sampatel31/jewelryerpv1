using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class ReportsView : UserControl
{
    public ReportsView(ReportsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
