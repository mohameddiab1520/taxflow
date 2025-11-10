using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;

namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Receipt-specific repository interface
/// </summary>
public interface IReceiptRepository : IRepository<Receipt>
{
    /// <summary>
    /// Gets receipts by status
    /// </summary>
    Task<IEnumerable<Receipt>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets receipts by terminal
    /// </summary>
    Task<IEnumerable<Receipt>> GetByTerminalAsync(string terminalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets receipts by date range
    /// </summary>
    Task<IEnumerable<Receipt>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets receipt with lines
    /// </summary>
    Task<Receipt?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets receipts pending submission to ETA
    /// </summary>
    Task<IEnumerable<Receipt>> GetPendingSubmissionAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if receipt number exists
    /// </summary>
    Task<bool> ReceiptNumberExistsAsync(string receiptNumber, CancellationToken cancellationToken = default);
}
