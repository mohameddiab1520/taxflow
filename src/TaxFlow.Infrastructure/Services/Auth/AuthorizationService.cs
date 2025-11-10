using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Auth;

/// <summary>
/// Authorization service for permission and role management
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<AuthorizationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode)
    {
        try
        {
            var permissions = await _permissionRepository.GetUserPermissionsAsync(userId);
            return permissions.Any(p => p.Code == permissionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permissionCode, userId);
            return false;
        }
    }

    public async Task<bool> HasRoleAsync(Guid userId, string roleName)
    {
        try
        {
            var roles = await _roleRepository.GetUserRolesAsync(userId);
            return roles.Any(r => r.Name == roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role {Role} for user {UserId}", roleName, userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var permissions = await _permissionRepository.GetUserPermissionsAsync(userId);
            return permissions.Select(p => p.Code).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var roles = await _roleRepository.GetUserRolesAsync(userId);
            return roles.Select(r => r.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid assignedBy)
    {
        try
        {
            var user = await _userRepository.GetWithRolesAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found {UserId}", userId);
                return false;
            }

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role not found {RoleId}", roleId);
                return false;
            }

            // Check if already assigned
            if (user.UserRoles.Any(ur => ur.RoleId == roleId))
            {
                _logger.LogWarning("Role {RoleName} already assigned to user {UserId}", role.Name, userId);
                return false;
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = assignedBy,
                AssignedAt = DateTime.UtcNow
            };

            user.UserRoles.Add(userRole);
            await _userRepository.UpdateAsync(user);

            await _auditService.LogActionAsync(
                assignedBy,
                "AssignRole",
                "User",
                userId,
                null,
                new { RoleName = role.Name });

            _logger.LogInformation("Role {RoleName} assigned to user {UserId} by {AssignedBy}",
                role.Name, userId, assignedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            return false;
        }
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        try
        {
            var user = await _userRepository.GetWithRolesAsync(userId);
            if (user == null)
                return false;

            var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
            if (userRole == null)
                return false;

            user.UserRoles.Remove(userRole);
            await _userRepository.UpdateAsync(user);

            await _auditService.LogActionAsync(userId, "RemoveRole", "User", userId);

            _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            return false;
        }
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleRepository.GetWithPermissionsAsync(roleId);
            if (role == null)
                return false;

            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null)
                return false;

            if (role.RolePermissions.Any(rp => rp.PermissionId == permissionId))
            {
                _logger.LogWarning("Permission already assigned to role");
                return false;
            }

            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                AssignedAt = DateTime.UtcNow
            };

            role.RolePermissions.Add(rolePermission);
            await _roleRepository.UpdateAsync(role);

            await _auditService.LogActionAsync(null, "AssignPermission", "Role", roleId);

            _logger.LogInformation("Permission {PermissionCode} assigned to role {RoleName}",
                permission.Code, role.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}",
                permissionId, roleId);
            return false;
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleRepository.GetWithPermissionsAsync(roleId);
            if (role == null)
                return false;

            var rolePermission = role.RolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
            if (rolePermission == null)
                return false;

            role.RolePermissions.Remove(rolePermission);
            await _roleRepository.UpdateAsync(role);

            await _auditService.LogActionAsync(null, "RemovePermission", "Role", roleId);

            _logger.LogInformation("Permission {PermissionId} removed from role {RoleId}",
                permissionId, roleId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}",
                permissionId, roleId);
            return false;
        }
    }
}
