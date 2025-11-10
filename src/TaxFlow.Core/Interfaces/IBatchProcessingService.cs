using TaxFlow.Core.Entities;

namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for batch processing of invoices and receipts with error recovery
/// </summary>
public interface IBatchProcessingService
{
    /// <summary>
    /// Processes a batch of invoices with signing and submission to ETA
    /// </summary>
    Task<BatchProcessingResult> ProcessInvoiceBatchAsync(
        List<Guid> invoiceIds,
        string certificateThumbprint,
        BatchProcessingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a batch of receipts with signing and submission to ETA
    /// </summary>
    Task<BatchProcessingResult> ProcessReceiptBatchAsync(
        List<Guid> receiptIds,
        string certificateThumbprint,
        BatchProcessingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed submissions
    /// </summary>
    Task<BatchProcessingResult> RetryFailedSubmissionsAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a batch processing job
    /// </summary>
    Task<BatchProcessingStatus> GetBatchStatusAsync(Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running batch job
    /// </summary>
    Task<bool> CancelBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Batch processing options
/// </summary>
public class BatchProcessingOptions
{
    /// <summary>
    /// Maximum number of documents to process in parallel
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 5;

    /// <summary>
    /// Number of retry attempts for failed submissions
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 2000;

    /// <summary>
    /// Whether to continue processing on errors
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Batch size for ETA submission (max 100)
    /// </summary>
    public int EtaBatchSize { get; set; } = 100;
}

/// <summary>
/// Result of batch processing
/// </summary>
public class BatchProcessingResult
{
    public Guid BatchId { get; set; }
    public int TotalDocuments { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public int Failed { get; set; }
    public List<BatchItemResult> ItemResults { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of a single item in a batch
/// </summary>
public class BatchItemResult
{
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? EtaLongId { get; set; }
    public int RetryAttempts { get; set; }
}

/// <summary>
/// Status of batch processing job
/// </summary>
public class BatchProcessingStatus
{
    public Guid BatchId { get; set; }
    public string Status { get; set; } = string.Empty; // Queued, Processing, Completed, Failed, Cancelled
    public int TotalDocuments { get; set; }
    public int ProcessedDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public double ProgressPercentage => TotalDocuments > 0 ? (ProcessedDocuments * 100.0 / TotalDocuments) : 0;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CurrentDocument { get; set; }
}
