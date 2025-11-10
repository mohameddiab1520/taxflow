using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaxFlow.Core.Interfaces;

namespace TaxFlow.Infrastructure.Services.Jobs;

/// <summary>
/// Background service for scheduled jobs
/// </summary>
public class ScheduledJobsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledJobsService> _logger;
    private Timer? _dailyBackupTimer;
    private Timer? _certificateCheckTimer;
    private Timer? _retryFailedSubmissionsTimer;

    public ScheduledJobsService(
        IServiceProvider serviceProvider,
        ILogger<ScheduledJobsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled Jobs Service started");

        // Schedule daily backup at 2 AM
        var now = DateTime.Now;
        var nextBackup = now.Date.AddDays(1).AddHours(2);
        var dueTime = nextBackup - now;

        _dailyBackupTimer = new Timer(
            async _ => await ExecuteDailyBackup(),
            null,
            dueTime,
            TimeSpan.FromHours(24));

        // Check certificate expiration every 6 hours
        _certificateCheckTimer = new Timer(
            async _ => await CheckCertificateExpiration(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromHours(6));

        // Retry failed submissions every hour
        _retryFailedSubmissionsTimer = new Timer(
            async _ => await RetryFailedSubmissions(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromHours(1));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ExecuteDailyBackup()
    {
        try
        {
            _logger.LogInformation("Starting daily backup job...");

            using var scope = _serviceProvider.CreateScope();
            var backupService = scope.ServiceProvider.GetService<IBackupService>();

            if (backupService != null)
            {
                var backupPath = await backupService.CreateBackupAsync();
                _logger.LogInformation("Daily backup completed: {BackupPath}", backupPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily backup job failed");
        }
    }

    private async Task CheckCertificateExpiration()
    {
        try
        {
            _logger.LogInformation("Checking certificate expiration...");

            using var scope = _serviceProvider.CreateScope();
            var certificateService = scope.ServiceProvider.GetService<ICertificateService>();

            if (certificateService != null)
            {
                var certificates = await certificateService.GetAllCertificatesAsync();

                foreach (var cert in certificates)
                {
                    var daysUntilExpiration = (cert.NotAfter - DateTime.Now).TotalDays;

                    if (daysUntilExpiration <= 30)
                    {
                        _logger.LogWarning(
                            "Certificate {Subject} expires in {Days} days",
                            cert.Subject,
                            (int)daysUntilExpiration);

                        // Send notification (implement notification service)
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Certificate expiration check failed");
        }
    }

    private async Task RetryFailedSubmissions()
    {
        try
        {
            _logger.LogInformation("Retrying failed ETA submissions...");

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var etaService = scope.ServiceProvider.GetRequiredService<IEtaSubmissionService>();

            // Get rejected invoices
            var invoices = await unitOfWork.Invoices.GetAllAsync();
            var rejectedInvoices = invoices
                .Where(i => i.Status == Core.Enums.DocumentStatus.Rejected)
                .Take(10) // Process max 10 at a time
                .ToList();

            foreach (var invoice in rejectedInvoices)
            {
                try
                {
                    _logger.LogInformation("Retrying submission for invoice {InvoiceNumber}", invoice.InvoiceNumber);

                    var result = await etaService.SubmitInvoiceAsync(invoice);

                    if (result.IsSuccess)
                    {
                        invoice.Status = Core.Enums.DocumentStatus.Submitted;
                        invoice.SubmittedAt = DateTime.UtcNow;
                        invoice.EtaUuid = result.Uuid;
                        invoice.EtaResponse = result.Message;

                        await unitOfWork.Invoices.UpdateAsync(invoice);
                        await unitOfWork.SaveChangesAsync();

                        _logger.LogInformation("Invoice {InvoiceNumber} submitted successfully on retry", invoice.InvoiceNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retry submission for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                }
            }

            _logger.LogInformation("Retry failed submissions completed. Processed {Count} invoices", rejectedInvoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retry failed submissions job failed");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled Jobs Service stopping");

        _dailyBackupTimer?.Change(Timeout.Infinite, 0);
        _certificateCheckTimer?.Change(Timeout.Infinite, 0);
        _retryFailedSubmissionsTimer?.Change(Timeout.Infinite, 0);

        _dailyBackupTimer?.Dispose();
        _certificateCheckTimer?.Dispose();
        _retryFailedSubmissionsTimer?.Dispose();

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _dailyBackupTimer?.Dispose();
        _certificateCheckTimer?.Dispose();
        _retryFailedSubmissionsTimer?.Dispose();
        base.Dispose();
    }
}
