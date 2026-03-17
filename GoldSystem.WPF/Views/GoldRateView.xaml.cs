using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class GoldRateView : UserControl
{
    public GoldRateView(GoldRateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
