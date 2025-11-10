using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;

namespace TaxFlow.Infrastructure.Services.Auth;

/// <summary>
/// Multi-Factor Authentication service using TOTP (Time-based One-Time Password)
/// </summary>
public class MfaService : IMfaService
{
    private readonly ILogger<MfaService> _logger;
    private const int TotpWindowSeconds = 30; // Standard TOTP window
    private const int QrCodePixelsPerModule = 20;

    public MfaService(ILogger<MfaService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a secret key for TOTP
    /// </summary>
    public async Task<MfaSetupResult> GenerateSecretKeyAsync(
        string userEmail,
        string issuer = "TaxFlow",
        CancellationToken cancellationToken = default)
    {
        var result = new MfaSetupResult();

        try
        {
            _logger.LogInformation("Generating MFA secret key for user {Email}", userEmail);

            await Task.Run(() =>
            {
                // Generate a random secret key (160 bits / 20 bytes)
                var secretKey = KeyGeneration.GenerateRandomKey(20);
                var base32Secret = Base32Encoding.ToString(secretKey);

                // Create TOTP URI for QR code
                var totpUri = $"otpauth://totp/{issuer}:{userEmail}?secret={base32Secret}&issuer={issuer}";

                result.SecretKey = base32Secret;
                result.TotpUri = totpUri;
                result.UserEmail = userEmail;
                result.Issuer = issuer;
                result.IsSuccess = true;

            }, cancellationToken);

            _logger.LogInformation("Successfully generated MFA secret key for user {Email}", userEmail);
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error generating MFA secret key for user {Email}", userEmail);
            return result;
        }
    }

    /// <summary>
    /// Generates a TOTP code using the secret key
    /// </summary>
    public async Task<string> GenerateTOTPCodeAsync(
        string secretKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating TOTP code");

            return await Task.Run(() =>
            {
                var secretBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(secretBytes, step: TotpWindowSeconds);
                var code = totp.ComputeTotp();

                _logger.LogDebug("Generated TOTP code: {Code}", code);
                return code;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TOTP code");
            throw;
        }
    }

    /// <summary>
    /// Validates a TOTP code against the secret key
    /// </summary>
    public async Task<MfaValidationResult> ValidateTOTPCodeAsync(
        string secretKey,
        string code,
        int toleranceWindowSteps = 1,
        CancellationToken cancellationToken = default)
    {
        var result = new MfaValidationResult
        {
            Code = code
        };

        try
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                result.IsValid = false;
                result.ErrorMessage = "Secret key cannot be null or empty";
                return result;
            }

            if (string.IsNullOrEmpty(code) || code.Length != 6)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid TOTP code format";
                return result;
            }

            _logger.LogDebug("Validating TOTP code");

            await Task.Run(() =>
            {
                var secretBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(secretBytes, step: TotpWindowSeconds);

                // Validate with time tolerance window (allows for clock drift)
                long timeStepMatched;
                result.IsValid = totp.VerifyTotp(
                    code,
                    out timeStepMatched,
                    window: new VerificationWindow(toleranceWindowSteps, toleranceWindowSteps));

                result.ValidatedAt = DateTime.UtcNow;

                if (result.IsValid)
                {
                    _logger.LogInformation("TOTP code validated successfully");
                }
                else
                {
                    _logger.LogWarning("TOTP code validation failed");
                    result.ErrorMessage = "Invalid or expired TOTP code";
                }

            }, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error validating TOTP code");
            return result;
        }
    }

    /// <summary>
    /// Generates a QR code image for TOTP setup
    /// </summary>
    public async Task<MfaQrCodeResult> GenerateQRCodeAsync(
        string totpUri,
        CancellationToken cancellationToken = default)
    {
        var result = new MfaQrCodeResult
        {
            TotpUri = totpUri
        };

        try
        {
            if (string.IsNullOrEmpty(totpUri))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "TOTP URI cannot be null or empty";
                return result;
            }

            _logger.LogDebug("Generating QR code for TOTP URI");

            await Task.Run(() =>
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(QrCodePixelsPerModule);

                result.QrCodeImage = qrCodeImage;
                result.QrCodeBase64 = Convert.ToBase64String(qrCodeImage);
                result.IsSuccess = true;

            }, cancellationToken);

            _logger.LogInformation("Successfully generated QR code");
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error generating QR code");
            return result;
        }
    }

    /// <summary>
    /// Generates backup codes for account recovery
    /// </summary>
    public async Task<List<string>> GenerateBackupCodesAsync(
        int count = 8,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating {Count} backup codes", count);

            return await Task.Run(() =>
            {
                var backupCodes = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    // Generate 8-character alphanumeric code
                    var code = GenerateBackupCode();
                    backupCodes.Add(code);
                }

                _logger.LogInformation("Successfully generated {Count} backup codes", count);
                return backupCodes;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes");
            throw;
        }
    }

    /// <summary>
    /// Gets the remaining time until current TOTP code expires
    /// </summary>
    public async Task<int> GetRemainingSecondsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var remainingSeconds = TotpWindowSeconds - (int)(unixTimestamp % TotpWindowSeconds);
            return remainingSeconds;
        }, cancellationToken);
    }

    /// <summary>
    /// Verifies if MFA is enabled for a user
    /// </summary>
    public async Task<bool> IsMfaEnabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Check database to see if user has MFA enabled
        // This requires User entity to have MfaEnabled and MfaSecretKey properties
        // For implementation, inject IUserRepository and check:
        // var user = await _userRepository.GetByIdAsync(userId);
        // return user?.MfaEnabled ?? false;

        await Task.CompletedTask;
        _logger.LogDebug("MFA status check for user {UserId} - requires User.MfaEnabled property", userId);
        return false; // Default to false until User entity is updated with MFA properties
    }

    /// <summary>
    /// Generates a random backup code
    /// </summary>
    private string GenerateBackupCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[8];

        using var rng = RandomNumberGenerator.Create();
        var data = new byte[8];
        rng.GetBytes(data);

        for (int i = 0; i < 8; i++)
        {
            code[i] = chars[data[i] % chars.Length];
        }

        return new string(code);
    }
}

/// <summary>
/// Interface for MFA service
/// </summary>
public interface IMfaService
{
    Task<MfaSetupResult> GenerateSecretKeyAsync(string userEmail, string issuer = "TaxFlow", CancellationToken cancellationToken = default);
    Task<string> GenerateTOTPCodeAsync(string secretKey, CancellationToken cancellationToken = default);
    Task<MfaValidationResult> ValidateTOTPCodeAsync(string secretKey, string code, int toleranceWindowSteps = 1, CancellationToken cancellationToken = default);
    Task<MfaQrCodeResult> GenerateQRCodeAsync(string totpUri, CancellationToken cancellationToken = default);
    Task<List<string>> GenerateBackupCodesAsync(int count = 8, CancellationToken cancellationToken = default);
    Task<int> GetRemainingSecondsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsMfaEnabledAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of MFA setup operation
/// </summary>
public class MfaSetupResult
{
    public bool IsSuccess { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string TotpUri { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of TOTP code validation
/// </summary>
public class MfaValidationResult
{
    public bool IsValid { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of QR code generation
/// </summary>
public class MfaQrCodeResult
{
    public bool IsSuccess { get; set; }
    public string TotpUri { get; set; } = string.Empty;
    public byte[] QrCodeImage { get; set; } = Array.Empty<byte>();
    public string QrCodeBase64 { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
