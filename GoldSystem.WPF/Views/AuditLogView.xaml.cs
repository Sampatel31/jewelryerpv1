using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class AuditLogView : UserControl
{
    public AuditLogView(AuditLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
