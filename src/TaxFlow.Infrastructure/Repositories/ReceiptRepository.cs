using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// Receipt repository implementation
/// </summary>
public class ReceiptRepository : Repository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(TaxFlowDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Receipt>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .Include(r => r.Lines)
            .Include(r => r.Customer)
            .OrderByDescending(r => r.DateTimeIssued)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Receipt>> GetByTerminalAsync(
        string terminalId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.TerminalId == terminalId)
            .Include(r => r.Lines)
            .OrderByDescending(r => r.DateTimeIssued)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Receipt>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.DateTimeIssued >= startDate && r.DateTimeIssued <= endDate)
            .Include(r => r.Lines)
            .Include(r => r.Customer)
            .OrderByDescending(r => r.DateTimeIssued)
            .ToListAsync(cancellationToken);
    }

    public async Task<Receipt?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Lines)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Receipt>> GetPendingSubmissionAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == DocumentStatus.Valid || r.Status == DocumentStatus.Failed)
            .Include(r => r.Lines)
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ReceiptNumberExistsAsync(
        string receiptNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            r => r.ReceiptNumber == receiptNumber,
            cancellationToken);
    }
}
