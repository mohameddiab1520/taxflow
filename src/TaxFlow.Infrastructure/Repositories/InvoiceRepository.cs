using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// Invoice repository implementation
/// </summary>
public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(TaxFlowDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.Status == status)
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .OrderByDescending(i => i.DateTimeIssued)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.CustomerId == customerId)
            .Include(i => i.Lines)
            .OrderByDescending(i => i.DateTimeIssued)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.DateTimeIssued >= startDate && i.DateTimeIssued <= endDate)
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .OrderByDescending(i => i.DateTimeIssued)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .ThenInclude(c => c!.Address)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetPendingSubmissionAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.Status == DocumentStatus.Valid || i.Status == DocumentStatus.Failed)
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .OrderBy(i => i.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> InvoiceNumberExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            i => i.InvoiceNumber == invoiceNumber,
            cancellationToken);
    }

    public async Task<Dictionary<DocumentStatus, int>> GetSubmissionStatsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.DateTimeIssued >= startDate && i.DateTimeIssued <= endDate)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }
}
