namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for background job processing
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a job for background processing
    /// </summary>
    Task<Guid> EnqueueJobAsync(BackgroundJobType jobType, object parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a background job
    /// </summary>
    Task<BackgroundJobStatus> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running job
    /// </summary>
    Task<bool> CancelJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending jobs
    /// </summary>
    Task<List<BackgroundJobInfo>> GetPendingJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all completed jobs
    /// </summary>
    Task<List<BackgroundJobInfo>> GetCompletedJobsAsync(int count = 100, CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of background jobs
/// </summary>
public enum BackgroundJobType
{
    InvoiceBatchSubmission,
    ReceiptBatchSubmission,
    ReportGeneration,
    DataExport,
    StatusSync,
    CertificateRenewal,
    DatabaseCleanup
}

/// <summary>
/// Status of a background job
/// </summary>
public class BackgroundJobStatus
{
    public Guid JobId { get; set; }
    public BackgroundJobType JobType { get; set; }
    public string Status { get; set; } = string.Empty; // Queued, Running, Completed, Failed, Cancelled
    public double ProgressPercentage { get; set; }
    public string? CurrentStep { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Background job information
/// </summary>
public class BackgroundJobInfo
{
    public Guid JobId { get; set; }
    public BackgroundJobType JobType { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Parameters { get; set; } = string.Empty;
}
