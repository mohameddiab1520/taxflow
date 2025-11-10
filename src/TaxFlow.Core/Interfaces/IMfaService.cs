namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Service for Two-Factor Authentication (TOTP)
/// </summary>
public interface IMfaService
{
    /// <summary>
    /// Generates a new TOTP secret key for a user
    /// </summary>
    Task<string> GenerateSecretKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a TOTP code for the given secret
    /// </summary>
    Task<string> GenerateTOTPCodeAsync(string secretKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a TOTP code
    /// </summary>
    Task<bool> ValidateTOTPCodeAsync(string secretKey, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a QR code for authenticator app setup
    /// </summary>
    Task<byte[]> GenerateQRCodeAsync(string secretKey, string userEmail, string issuer = "TaxFlow", CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates backup codes for MFA
    /// </summary>
    Task<List<string>> GenerateBackupCodesAsync(int count = 8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets remaining seconds until next TOTP code
    /// </summary>
    Task<int> GetRemainingSecondsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if MFA is enabled for a user
    /// </summary>
    Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);
}
