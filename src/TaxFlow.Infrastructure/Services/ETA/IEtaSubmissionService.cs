using TaxFlow.Core.Entities;

namespace TaxFlow.Infrastructure.Services.ETA;

/// <summary>
/// ETA submission service interface for submitting invoices and receipts
/// </summary>
public interface IEtaSubmissionService
{
    /// <summary>
    /// Submits a single invoice to ETA
    /// </summary>
    Task<EtaSubmissionResult> SubmitInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits multiple invoices to ETA in batch
    /// </summary>
    Task<List<EtaSubmissionResult>> SubmitInvoiceBatchAsync(List<Invoice> invoices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a single receipt to ETA
    /// </summary>
    Task<EtaSubmissionResult> SubmitReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits multiple receipts to ETA in batch
    /// </summary>
    Task<List<EtaSubmissionResult>> SubmitReceiptBatchAsync(List<Receipt> receipts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the submission status from ETA
    /// </summary>
    Task<EtaSubmissionStatus> GetSubmissionStatusAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a submitted document
    /// </summary>
    Task<bool> CancelDocumentAsync(string etaLongId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// ETA submission result
/// </summary>
public class EtaSubmissionResult
{
    public bool IsSuccess { get; set; }
    public Guid? SubmissionId { get; set; }
    public string? LongId { get; set; }
    public string? InternalId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ETA submission status
/// </summary>
public class EtaSubmissionStatus
{
    public string Status { get; set; } = string.Empty;
    public string? StatusReason { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
