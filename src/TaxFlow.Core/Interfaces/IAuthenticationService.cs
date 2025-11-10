using TaxFlow.Core.Entities;

namespace TaxFlow.Core.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string username, string password, string ipAddress);
    Task<bool> LogoutAsync(Guid userId);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ConfirmEmailAsync(Guid userId, string token);
    Task<User?> GetCurrentUserAsync();
}

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode);
    Task<bool> HasRoleAsync(Guid userId, string roleName);
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid assignedBy);
    Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
}

public interface IAuditService
{
    Task LogActionAsync(Guid? userId, string action, string entityType, Guid? entityId = null, object? oldValues = null, object? newValues = null);
    Task<List<AuditLog>> GetUserActivityAsync(Guid userId, int count = 100);
    Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId);
}

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
