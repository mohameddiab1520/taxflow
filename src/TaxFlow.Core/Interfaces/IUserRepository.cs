using TaxFlow.Core.Entities;

namespace TaxFlow.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithRolesAsync(Guid userId);
    Task<List<User>> GetByRoleAsync(string roleName);
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<bool> IsEmailAvailableAsync(string email);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
    Task<Role?> GetWithPermissionsAsync(Guid roleId);
    Task<List<Role>> GetSystemRolesAsync();
    Task<List<Role>> GetUserRolesAsync(Guid userId);
}

public interface IPermissionRepository : IRepository<Permission>
{
    Task<Permission?> GetByCodeAsync(string code);
    Task<List<Permission>> GetByModuleAsync(string module);
    Task<List<Permission>> GetRolePermissionsAsync(Guid roleId);
    Task<List<Permission>> GetUserPermissionsAsync(Guid userId);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<List<AuditLog>> GetByUserAsync(Guid userId, int count = 100);
    Task<List<AuditLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task<List<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<AuditLog>> GetByActionAsync(string action);
}
