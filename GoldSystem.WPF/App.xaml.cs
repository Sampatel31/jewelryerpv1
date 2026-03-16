using GoldSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;

namespace GoldSystem.WPF;

/// <summary>
/// Interaction logic for App.xaml – sets up the .NET Generic Host with
/// dependency injection, logging, and EF Core before launching the main window.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "goldsystem-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context);
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        // Database
        services.AddDbContext<GoldDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoldSystem.db")}"));

        // Main window
        services.AddSingleton<MainWindow>();

        // Navigation
        services.AddSingleton<Services.NavigationService>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

