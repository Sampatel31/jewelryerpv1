using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
