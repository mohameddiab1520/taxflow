using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace TaxFlow.Infrastructure.Services.Auth;

/// <summary>
/// Authentication service for user login, logout, and password management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthenticationService> _logger;
    private User? _currentUser;

    public AuthenticationService(
        IUserRepository userRepository,
        IAuditService auditService,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password, string ipAddress)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(username);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - {Username}", username);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid username or password"
                };
            }

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login failed: Account locked - {Username}", username);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Account is locked until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm}"
                };
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account inactive - {Username}", username);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Account is inactive"
                };
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                // Lock account after 5 failed attempts
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("Account locked due to multiple failed login attempts - {Username}", username);
                }

                await _userRepository.UpdateAsync(user);

                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid username or password"
                };
            }

            // Login successful
            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;
            user.LockoutEnd = null;

            await _userRepository.UpdateAsync(user);
            _currentUser = user;

            // Log the login
            await _auditService.LogActionAsync(user.Id, "Login", "User", user.Id);

            _logger.LogInformation("User logged in successfully - {Username}", username);

            return new AuthenticationResult
            {
                IsSuccess = true,
                User = user,
                AccessToken = GenerateToken(user),
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during login"
            };
        }
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        try
        {
            await _auditService.LogActionAsync(userId, "Logout", "User", userId);
            _currentUser = null;
            _logger.LogInformation("User logged out - {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return false;
        }
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        // TODO: Implement refresh token logic
        await Task.CompletedTask;
        return new AuthenticationResult { IsSuccess = false, ErrorMessage = "Not implemented" };
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            if (!VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();

            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(userId, "ChangePassword", "User", userId);

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return false;

            // Generate random password
            var newPassword = GenerateRandomPassword();
            user.PasswordHash = HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();

            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(user.Id, "ResetPassword", "User", user.Id);

            // TODO: Send email with new password
            _logger.LogInformation("Password reset for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", email);
            return false;
        }
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.EmailConfirmed = true;
            await _userRepository.UpdateAsync(user);
            await _auditService.LogActionAsync(userId, "ConfirmEmail", "User", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
            return false;
        }
    }

    public Task<User?> GetCurrentUserAsync()
    {
        return Task.FromResult(_currentUser);
    }

    // Helper methods
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hash = HashPassword(password);
        return hash == passwordHash;
    }

    private static string GenerateToken(User user)
    {
        // Simple token generation (in production, use JWT)
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{DateTime.UtcNow:O}"));
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
