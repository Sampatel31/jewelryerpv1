using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class InventoryView : UserControl
{
    public InventoryView(InventoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
