using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// User repository implementation
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(TaxFlowDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetWithRolesAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<List<User>> GetByRoleAsync(string roleName)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName))
            .ToListAsync();
    }

    public async Task<bool> IsUsernameAvailableAsync(string username)
    {
        return !await _dbSet.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        return !await _dbSet.AnyAsync(u => u.Email == email);
    }
}
