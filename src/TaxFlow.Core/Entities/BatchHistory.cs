namespace TaxFlow.Core.Entities;

/// <summary>
/// Tracks batch processing history and status
/// </summary>
public class BatchHistory : BaseEntity
{
    public Guid BatchId { get; set; }
    public string BatchType { get; set; } = string.Empty; // Invoice or Receipt
    public int TotalDocuments { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public int Failed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Cancelled, Failed
    public string? ErrorMessage { get; set; }
    public string? CertificateThumbprint { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int MaxDegreeOfParallelism { get; set; }

    // Navigation
    public ICollection<BatchItemHistory> ItemHistories { get; set; } = new List<BatchItemHistory>();
}

/// <summary>
/// Tracks individual document processing within a batch
/// </summary>
public class BatchItemHistory : BaseEntity
{
    public Guid BatchHistoryId { get; set; }
    public Guid DocumentId { get; set; }
    public string? DocumentNumber { get; set; }
    public bool IsSuccess { get; set; }
    public int RetryAttempts { get; set; }
    public string? ErrorMessage { get; set; }
    public string? EtaLongId { get; set; }
    public DateTime ProcessedAt { get; set; }

    // Navigation
    public BatchHistory BatchHistory { get; set; } = null!;
}
