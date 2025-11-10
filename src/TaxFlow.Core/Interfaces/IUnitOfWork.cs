using TaxFlow.Core.Entities;

namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Unit of Work interface for coordinating multiple repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Invoice repository
    /// </summary>
    IInvoiceRepository Invoices { get; }

    /// <summary>
    /// Receipt repository
    /// </summary>
    IReceiptRepository Receipts { get; }

    /// <summary>
    /// Customer repository
    /// </summary>
    IRepository<Customer> Customers { get; }

    /// <summary>
    /// Commits all changes to the database
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
