using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class CustomerView : UserControl
{
    public CustomerView(CustomerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
