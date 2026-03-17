using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class BillingView : UserControl
{
    public BillingView(BillingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
