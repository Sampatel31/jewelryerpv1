using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class AIInsightsView : UserControl
{
    public AIInsightsView(AIInsightsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
