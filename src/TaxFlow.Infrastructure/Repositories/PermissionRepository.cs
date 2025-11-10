using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// Permission repository implementation
/// </summary>
public class PermissionRepository : Repository<Permission>, IPermissionRepository
{
    public PermissionRepository(TaxFlowDbContext context) : base(context)
    {
    }

    public async Task<Permission?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<List<Permission>> GetByModuleAsync(string module)
    {
        return await _dbSet
            .Where(p => p.Module == module)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(Guid roleId)
    {
        return await _dbSet
            .Where(p => p.RolePermissions.Any(rp => rp.RoleId == roleId))
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetUserPermissionsAsync(Guid userId)
    {
        return await _dbSet
            .Where(p => p.RolePermissions.Any(rp => rp.Role.UserRoles.Any(ur => ur.UserId == userId)))
            .Distinct()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}
