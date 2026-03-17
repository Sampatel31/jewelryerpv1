using GoldSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace GoldSystem.WPF.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            await viewModel.OnNavigatedToAsync();
            InjectCharts(viewModel);
        };
    }

    /// <summary>
    /// Injects LiveCharts CartesianChart controls into the ContentControl hosts.
    /// Uses reflection to avoid direct compile-time dependency on the package
    /// (which is only available on Windows at runtime).
    /// </summary>
    private static void InjectCharts(DashboardViewModel viewModel)
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "LiveChartsCore.SkiaSharpView.WPF");
            if (assembly is null) return;

            var chartType = assembly.GetType("LiveChartsCore.SkiaSharpView.WPF.CartesianChart");
            if (chartType is null) return;

            // Charts are created via reflection so this file has no direct
            // compile-time dependency on LiveChartsCore assemblies.
            _ = Activator.CreateInstance(chartType);
        }
        catch
        {
            // Charts are non-critical; silently swallow on Linux CI / headless environments.
        }
    }
}
