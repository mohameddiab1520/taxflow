using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Desktop.ViewModels.Settings;

/// <summary>
/// View model for batch submission of invoices and receipts
/// </summary>
public partial class BatchSubmissionViewModel : ViewModelBase
{
    private readonly IBatchProcessingService _batchService;
    private readonly ICertificateService _certificateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchSubmissionViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BatchJobInfo> _batchJobs = new();

    [ObservableProperty]
    private BatchJobInfo? _selectedBatch;

    [ObservableProperty]
    private string _selectedCertificateThumbprint = string.Empty;

    [ObservableProperty]
    private int _maxDegreeOfParallelism = 5;

    [ObservableProperty]
    private int _maxRetryAttempts = 3;

    [ObservableProperty]
    private bool _continueOnError = true;

    [ObservableProperty]
    private DateTime? _filterStartDate;

    [ObservableProperty]
    private DateTime? _filterEndDate;

    [ObservableProperty]
    private string? _filterStatus = "Draft";

    [ObservableProperty]
    private int _pendingInvoiceCount;

    [ObservableProperty]
    private int _pendingReceiptCount;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public List<string> StatusOptions { get; } = new() { "Draft", "Rejected", "All" };

    public BatchSubmissionViewModel(
        IBatchProcessingService batchService,
        ICertificateService certificateService,
        IUnitOfWork unitOfWork,
        ILogger<BatchSubmissionViewModel> logger)
    {
        _batchService = batchService;
        _certificateService = certificateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadPendingCountsAsync();
    }

    /// <summary>
    /// Loads pending invoice and receipt counts
    /// </summary>
    [RelayCommand]
    private async Task LoadPendingCountsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var receipts = await _unitOfWork.Receipts.GetAllAsync();

            var query = invoices.AsEnumerable();

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "All")
            {
                query = query.Where(i => i.Status.ToString() == FilterStatus);
            }

            if (FilterStartDate.HasValue)
            {
                query = query.Where(i => i.DateTimeIssued >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(i => i.DateTimeIssued <= FilterEndDate.Value);
            }

            PendingInvoiceCount = query.Count();

            var receiptQuery = receipts.AsEnumerable();
            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "All")
            {
                receiptQuery = receiptQuery.Where(r => r.Status.ToString() == FilterStatus);
            }

            if (FilterStartDate.HasValue)
            {
                receiptQuery = receiptQuery.Where(r => r.DateTimeIssued >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                receiptQuery = receiptQuery.Where(r => r.DateTimeIssued <= FilterEndDate.Value);
            }

            PendingReceiptCount = receiptQuery.Count();

            _logger.LogInformation(
                "Pending counts: {Invoices} invoices, {Receipts} receipts",
                PendingInvoiceCount,
                PendingReceiptCount);

        }, "Loading pending counts...");
    }

    /// <summary>
    /// Submits invoices in batch
    /// </summary>
    [RelayCommand]
    private async Task SubmitInvoiceBatchAsync()
    {
        if (string.IsNullOrEmpty(SelectedCertificateThumbprint))
        {
            SetError("Please select a certificate for signing");
            return;
        }

        if (PendingInvoiceCount == 0)
        {
            SetError("No pending invoices to submit");
            return;
        }

        await ExecuteAsync(async () =>
        {
            IsProcessing = true;
            ProgressPercentage = 0;
            StatusMessage = "Preparing batch submission...";

            // Get invoice IDs
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var query = invoices.AsEnumerable();

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "All")
            {
                query = query.Where(i => i.Status.ToString() == FilterStatus);
            }

            if (FilterStartDate.HasValue)
            {
                query = query.Where(i => i.DateTimeIssued >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(i => i.DateTimeIssued <= FilterEndDate.Value);
            }

            var invoiceIds = query.Select(i => i.Id).ToList();

            StatusMessage = $"Submitting {invoiceIds.Count} invoices...";

            var options = new BatchProcessingOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                MaxRetryAttempts = MaxRetryAttempts,
                ContinueOnError = ContinueOnError
            };

            var result = await _batchService.ProcessInvoiceBatchAsync(
                invoiceIds,
                SelectedCertificateThumbprint,
                options);

            ProgressPercentage = 100;
            IsProcessing = false;

            StatusMessage = $"Batch completed: {result.SuccessfullyProcessed}/{result.TotalDocuments} successful " +
                          $"({result.Failed} failed) in {result.Duration.TotalSeconds:N1}s";

            _logger.LogInformation(
                "Invoice batch {BatchId} completed: {Success}/{Total}",
                result.BatchId,
                result.SuccessfullyProcessed,
                result.TotalDocuments);

            // Add to batch jobs list
            BatchJobs.Add(new BatchJobInfo
            {
                BatchId = result.BatchId,
                DocumentType = "Invoice",
                TotalDocuments = result.TotalDocuments,
                SuccessfulDocuments = result.SuccessfullyProcessed,
                FailedDocuments = result.Failed,
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt,
                Duration = result.Duration
            });

            await LoadPendingCountsAsync();

        }, "Submitting invoice batch...");
    }

    /// <summary>
    /// Submits receipts in batch
    /// </summary>
    [RelayCommand]
    private async Task SubmitReceiptBatchAsync()
    {
        if (string.IsNullOrEmpty(SelectedCertificateThumbprint))
        {
            SetError("Please select a certificate for signing");
            return;
        }

        if (PendingReceiptCount == 0)
        {
            SetError("No pending receipts to submit");
            return;
        }

        await ExecuteAsync(async () =>
        {
            IsProcessing = true;
            ProgressPercentage = 0;
            StatusMessage = "Preparing receipt batch submission...";

            var receipts = await _unitOfWork.Receipts.GetAllAsync();
            var query = receipts.AsEnumerable();

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "All")
            {
                query = query.Where(r => r.Status.ToString() == FilterStatus);
            }

            if (FilterStartDate.HasValue)
            {
                query = query.Where(r => r.DateTimeIssued >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(r => r.DateTimeIssued <= FilterEndDate.Value);
            }

            var receiptIds = query.Select(r => r.Id).ToList();

            StatusMessage = $"Submitting {receiptIds.Count} receipts...";

            var options = new BatchProcessingOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                MaxRetryAttempts = MaxRetryAttempts,
                ContinueOnError = ContinueOnError
            };

            var result = await _batchService.ProcessReceiptBatchAsync(
                receiptIds,
                SelectedCertificateThumbprint,
                options);

            ProgressPercentage = 100;
            IsProcessing = false;

            StatusMessage = $"Batch completed: {result.SuccessfullyProcessed}/{result.TotalDocuments} successful " +
                          $"({result.Failed} failed) in {result.Duration.TotalSeconds:N1}s";

            BatchJobs.Add(new BatchJobInfo
            {
                BatchId = result.BatchId,
                DocumentType = "Receipt",
                TotalDocuments = result.TotalDocuments,
                SuccessfulDocuments = result.SuccessfullyProcessed,
                FailedDocuments = result.Failed,
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt,
                Duration = result.Duration
            });

            await LoadPendingCountsAsync();

        }, "Submitting receipt batch...");
    }

    /// <summary>
    /// Refreshes the view
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadPendingCountsAsync();
    }

    /// <summary>
    /// Property changed handlers for auto-refresh
    /// </summary>
    partial void OnFilterStatusChanged(string? value)
    {
        _ = LoadPendingCountsAsync();
    }

    partial void OnFilterStartDateChanged(DateTime? value)
    {
        _ = LoadPendingCountsAsync();
    }

    partial void OnFilterEndDateChanged(DateTime? value)
    {
        _ = LoadPendingCountsAsync();
    }
}

/// <summary>
/// Batch job information for display
/// </summary>
public class BatchJobInfo
{
    public Guid BatchId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int SuccessfulDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status => CompletedAt.HasValue ? "Completed" : "Running";
    public double SuccessRate => TotalDocuments > 0 ? (SuccessfulDocuments * 100.0 / TotalDocuments) : 0;
}
