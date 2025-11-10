using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TaxFlow.Infrastructure.Data;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Repositories;
using TaxFlow.Desktop.ViewModels;
using TaxFlow.Desktop.Views;
using TaxFlow.Desktop.Services;
using StackExchange.Redis;
using TaxFlow.Infrastructure.Caching;
using TaxFlow.Application.Services;
using TaxFlow.Infrastructure.Services.ETA;
using TaxFlow.Desktop.ViewModels.Invoices;

namespace TaxFlow.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/taxflow-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database Contexts
        services.AddDbContext<TaxFlowDbContext>(options =>
        {
            options.UseSqlite("Data Source=taxflow.db");
            options.EnableSensitiveDataLogging(false);
        });

        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            // PostgreSQL connection - configure in production
            options.UseNpgsql("Host=localhost;Database=taxflow_analytics;Username=postgres;Password=postgres");
        });

        // Redis Cache
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse("localhost:6379");
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();

        // Application Services
        services.AddScoped<ITaxCalculationService, TaxCalculationService>();
        services.AddHttpClient<IEtaAuthenticationService, EtaAuthenticationService>();
        services.AddHttpClient<IEtaSubmissionService, EtaSubmissionService>();

        // UI Services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<InvoiceListViewModel>();
        services.AddTransient<InvoiceViewModel>();
        services.AddTransient<ReceiptListViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();

        // Initialize database
        await InitializeDatabaseAsync();

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TaxFlowDbContext>();

            // Create database if it doesn't exist
            await context.Database.EnsureCreatedAsync();

            Log.Information("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing database");
            MessageBox.Show(
                $"Failed to initialize database: {ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public static T GetService<T>() where T : notnull
    {
        return ((App)Current)._host.Services.GetRequiredService<T>();
    }
}
