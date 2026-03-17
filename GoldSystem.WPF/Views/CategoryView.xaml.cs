using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class CategoryView : UserControl
{
    public CategoryView(CategoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
