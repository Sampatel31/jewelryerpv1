using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class VendorView : UserControl
{
    public VendorView(VendorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
