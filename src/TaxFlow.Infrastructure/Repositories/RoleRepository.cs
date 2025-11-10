using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// Role repository implementation
/// </summary>
public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(TaxFlowDbContext context) : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<Role?> GetWithPermissionsAsync(Guid roleId)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId);
    }

    public async Task<List<Role>> GetSystemRolesAsync()
    {
        return await _dbSet
            .Where(r => r.IsSystemRole)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync();
    }

    public async Task<List<Role>> GetUserRolesAsync(Guid userId)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.UserRoles.Any(ur => ur.UserId == userId))
            .ToListAsync();
    }
}
