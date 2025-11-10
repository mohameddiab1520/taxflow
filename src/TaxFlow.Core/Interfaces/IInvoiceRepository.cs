using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;

namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Invoice-specific repository interface
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice>
{
    /// <summary>
    /// Gets invoices by status
    /// </summary>
    Task<IEnumerable<Invoice>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices by customer
    /// </summary>
    Task<IEnumerable<Invoice>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices by date range
    /// </summary>
    Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoice with lines and customer details
    /// </summary>
    Task<Invoice?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices pending submission to ETA
    /// </summary>
    Task<IEnumerable<Invoice>> GetPendingSubmissionAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if invoice number exists
    /// </summary>
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets submission statistics for a date range
    /// </summary>
    Task<Dictionary<DocumentStatus, int>> GetSubmissionStatsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
