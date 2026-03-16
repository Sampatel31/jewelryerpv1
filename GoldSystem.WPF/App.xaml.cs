using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Repositories;
using GoldSystem.Data.Services;
using GoldSystem.RateEngine;
using GoldSystem.RateEngine.Interfaces;
using GoldSystem.RateEngine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            .ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
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
        services.AddDbContextFactory<GoldDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoldSystem.db")}"),
            ServiceLifetime.Singleton);

        // Configuration options
        services.Configure<GoldRateOptions>(
            context.Configuration.GetSection(GoldRateOptions.SectionName));
        services.Configure<RateBroadcastOptions>(
            context.Configuration.GetSection(RateBroadcastOptions.SectionName));

        // Rate Engine services
        services.AddSingleton<IRateSource, McxRateScraper>();
        services.AddSingleton<RateRepository>();
        services.AddSingleton<RateBroadcaster>();
        services.AddSingleton<RateChangedEventPublisher>();
        services.AddSingleton<RateConfigurationService>();
        services.AddHostedService<RateSyncBackgroundService>();
        services.AddHostedService<RateListenerService>();
        services.AddSingleton<ManualRateEntryService>();

        // Main window
        services.AddSingleton<MainWindow>();

        // Navigation
        services.AddSingleton<Services.NavigationService>();

        // Billing Engine services
        services.AddScoped<IBillingEngine, BillingEngine>();
        services.AddScoped<IBillNumberGenerator, BillNumberGeneratorService>();
        services.AddScoped<IBillingValidationService, BillingValidationService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<GoldPriceCalculator>();

        // Repository Pattern / Data Access Layer
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<InventoryQueryService>();
        services.AddScoped<BillingQueryService>();
        services.AddScoped<SyncQueryService>();
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

