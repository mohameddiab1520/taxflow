using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// AuditLog repository implementation
/// </summary>
public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(TaxFlowDbContext context) : base(context)
    {
    }

    public async Task<List<AuditLog>> GetByUserAsync(Guid userId, int count = 100)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .Include(a => a.User)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetByEntityAsync(string entityType, Guid entityId)
    {
        return await _dbSet
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Include(a => a.User)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .OrderByDescending(a => a.CreatedAt)
            .Include(a => a.User)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetByActionAsync(string action)
    {
        return await _dbSet
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.CreatedAt)
            .Include(a => a.User)
            .ToListAsync();
    }
}
