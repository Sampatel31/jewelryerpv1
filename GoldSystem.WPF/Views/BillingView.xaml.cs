using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoldSystem.WPF.Views;

public partial class BillingView : UserControl
{
    public BillingView(BillingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // F8 focuses the barcode scanner input box from anywhere in the view
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F8)
            {
                BarcodeBox.Focus();
                BarcodeBox.SelectAll();
                e.Handled = true;
            }
        };
    }
}
