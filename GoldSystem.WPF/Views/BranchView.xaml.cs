using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class BranchView : UserControl
{
    public BranchView(BranchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
