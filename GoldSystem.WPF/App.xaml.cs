using GoldSystem.AI.Services;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Repositories;
using GoldSystem.Data.Services;
using GoldSystem.RateEngine;
using GoldSystem.RateEngine.Interfaces;
using GoldSystem.RateEngine.Services;
using GoldSystem.Reports.Services;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using GoldSystem.WPF.Views;
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
/// dependency injection, logging, and EF Core before launching the shell window.
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

        var shell = _host.Services.GetRequiredService<ShellWindow>();
        shell.Show();
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

        // App-wide state & shell services
        services.AddSingleton<AppState>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<StatusIndicatorService>();

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

        // AI / ML.NET services
        services.AddSingleton<ISalesForecastService, SalesForecastService>();
        services.AddSingleton<ISlowStockDetectorService, SlowStockDetectorService>();
        services.AddSingleton<IRateTrendAnalyzerService, RateTrendAnalyzerService>();
        services.AddSingleton<IRestockSuggestionsService, RestockSuggestionsService>();
        services.AddSingleton<IAnomalyDetectorService, AnomalyDetectorService>();
        services.AddHostedService<ModelTrainingScheduler>();

        // Shell window and ViewModel
        services.AddSingleton<ShellWindow>();
        services.AddSingleton<ShellViewModel>();

        // Dashboard
        services.AddTransient<DashboardView>();
        services.AddTransient<DashboardViewModel>();

        // All other Views and ViewModels (scoped to navigation lifetime)
        services.AddTransient<BillingView>();
        services.AddTransient<BillingViewModel>();
        services.AddScoped<BillingScreenService>();
        services.AddSingleton<IBillPdfService, BillPdfService>();

        services.AddTransient<InventoryView>();
        services.AddTransient<InventoryViewModel>();
        services.AddScoped<StockTransferService>();

        services.AddTransient<CustomerView>();
        services.AddTransient<CustomerViewModel>();

        services.AddTransient<GoldRateView>();
        services.AddTransient<GoldRateViewModel>();

        services.AddTransient<ReportsView>();
        services.AddTransient<ReportsViewModel>();

        services.AddTransient<SettingsView>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<VendorView>();
        services.AddTransient<VendorViewModel>();

        services.AddTransient<CategoryView>();
        services.AddTransient<CategoryViewModel>();

        services.AddTransient<SyncStatusView>();
        services.AddTransient<SyncStatusViewModel>();

        services.AddTransient<AIInsightsView>();
        services.AddTransient<AIInsightsViewModel>();

        services.AddTransient<AuditLogView>();
        services.AddTransient<AuditLogViewModel>();

        services.AddTransient<UserManagementView>();
        services.AddTransient<UserManagementViewModel>();

        services.AddTransient<BranchView>();
        services.AddTransient<BranchViewModel>();

        services.AddTransient<AboutView>();
        services.AddTransient<AboutViewModel>();
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

