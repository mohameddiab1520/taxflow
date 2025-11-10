using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Security;

/// <summary>
/// Service for encryption and decryption operations using AES-256
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _masterKey;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Get master encryption key from configuration or generate new one
        var masterKeyBase64 = _configuration["Encryption:MasterKey"];
        if (string.IsNullOrEmpty(masterKeyBase64))
        {
            _logger.LogWarning("No master encryption key found in configuration. Using generated key.");
            _masterKey = GenerateKeyBytes();
        }
        else
        {
            try
            {
                _masterKey = Convert.FromBase64String(masterKeyBase64);
                if (_masterKey.Length != 32)
                {
                    _logger.LogWarning("Master key is not 256 bits. Generating new key.");
                    _masterKey = GenerateKeyBytes();
                }
            }
            catch
            {
                _logger.LogWarning("Invalid master key in configuration. Generating new key.");
                _masterKey = GenerateKeyBytes();
            }
        }
    }

    /// <summary>
    /// Encrypts a string using AES-256-GCM
    /// </summary>
    public async Task<EncryptionResult> EncryptStringAsync(
        string plainText,
        CancellationToken cancellationToken = default)
    {
        var result = new EncryptionResult();

        try
        {
            if (string.IsNullOrEmpty(plainText))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Plain text cannot be null or empty";
                return result;
            }

            _logger.LogDebug("Encrypting string of length {Length}", plainText.Length);

            await Task.Run(() =>
            {
                // Generate a random nonce (96 bits for GCM)
                var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                RandomNumberGenerator.Fill(nonce);

                // Convert plain text to bytes
                var plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Allocate buffers
                var cipherBytes = new byte[plainBytes.Length];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];

                // Encrypt using AES-GCM
                using var aesGcm = new AesGcm(_masterKey, tag.Length);
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

                // Combine nonce + tag + cipher for storage
                var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
                Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
                Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
                Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

                result.EncryptedText = Convert.ToBase64String(combined);
                result.IsSuccess = true;

            }, cancellationToken);

            _logger.LogDebug("Successfully encrypted string");
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error encrypting string");
            return result;
        }
    }

    /// <summary>
    /// Decrypts a string using AES-256-GCM
    /// </summary>
    public async Task<DecryptionResult> DecryptStringAsync(
        string encryptedText,
        CancellationToken cancellationToken = default)
    {
        var result = new DecryptionResult();

        try
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Encrypted text cannot be null or empty";
                return result;
            }

            _logger.LogDebug("Decrypting string");

            await Task.Run(() =>
            {
                // Decode from Base64
                var combined = Convert.FromBase64String(encryptedText);

                // Extract nonce, tag, and cipher
                var nonceSize = AesGcm.NonceByteSizes.MaxSize;
                var tagSize = AesGcm.TagByteSizes.MaxSize;

                if (combined.Length < nonceSize + tagSize)
                {
                    throw new CryptographicException("Invalid encrypted data format");
                }

                var nonce = new byte[nonceSize];
                var tag = new byte[tagSize];
                var cipherBytes = new byte[combined.Length - nonceSize - tagSize];

                Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
                Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
                Buffer.BlockCopy(combined, nonceSize + tagSize, cipherBytes, 0, cipherBytes.Length);

                // Decrypt using AES-GCM
                var plainBytes = new byte[cipherBytes.Length];

                using var aesGcm = new AesGcm(_masterKey, tag.Length);
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

                result.DecryptedText = Encoding.UTF8.GetString(plainBytes);
                result.IsSuccess = true;

            }, cancellationToken);

            _logger.LogDebug("Successfully decrypted string");
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error decrypting string");
            return result;
        }
    }

    /// <summary>
    /// Generates a new encryption key (256-bit)
    /// </summary>
    public async Task<string> GenerateKeyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating new 256-bit encryption key");

            var key = await Task.Run(() =>
            {
                var keyBytes = GenerateKeyBytes();
                return Convert.ToBase64String(keyBytes);
            }, cancellationToken);

            _logger.LogInformation("Successfully generated new encryption key");
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating encryption key");
            throw;
        }
    }

    /// <summary>
    /// Hashes a password using SHA-256 with salt
    /// </summary>
    public async Task<PasswordHashResult> HashPasswordAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        var result = new PasswordHashResult();

        try
        {
            if (string.IsNullOrEmpty(password))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Password cannot be null or empty";
                return result;
            }

            _logger.LogDebug("Hashing password");

            await Task.Run(() =>
            {
                // Generate a random salt (128 bits)
                var salt = new byte[16];
                RandomNumberGenerator.Fill(salt);

                // Use PBKDF2 (RFC 2898) for password hashing - more secure than simple SHA-256
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations: 100000, // 100,000 iterations for security
                    HashAlgorithmName.SHA256);

                var hash = pbkdf2.GetBytes(32); // 256-bit hash

                // Combine salt + hash
                var combined = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);

                result.HashedPassword = Convert.ToBase64String(combined);
                result.Salt = Convert.ToBase64String(salt);
                result.IsSuccess = true;

            }, cancellationToken);

            _logger.LogDebug("Successfully hashed password");
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error hashing password");
            return result;
        }
    }

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    public async Task<bool> VerifyPasswordAsync(
        string password,
        string hashedPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            _logger.LogDebug("Verifying password");

            return await Task.Run(() =>
            {
                // Decode the stored hash
                var combined = Convert.FromBase64String(hashedPassword);

                if (combined.Length != 48) // 16 bytes salt + 32 bytes hash
                {
                    return false;
                }

                // Extract salt and hash
                var salt = new byte[16];
                var hash = new byte[32];
                Buffer.BlockCopy(combined, 0, salt, 0, 16);
                Buffer.BlockCopy(combined, 16, hash, 0, 32);

                // Hash the input password with the same salt
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations: 100000,
                    HashAlgorithmName.SHA256);

                var testHash = pbkdf2.GetBytes(32);

                // Compare hashes using constant-time comparison
                return CryptographicOperations.FixedTimeEquals(hash, testHash);

            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    /// <summary>
    /// Generates random encryption key bytes
    /// </summary>
    private byte[] GenerateKeyBytes()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// Encrypts data using AES-256-CBC (alternative method for compatibility)
    /// </summary>
    public async Task<EncryptionResult> EncryptBytesAsync(
        byte[] plainBytes,
        CancellationToken cancellationToken = default)
    {
        var result = new EncryptionResult();

        try
        {
            if (plainBytes == null || plainBytes.Length == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Plain bytes cannot be null or empty";
                return result;
            }

            _logger.LogDebug("Encrypting {Size} bytes", plainBytes.Length);

            await Task.Run(() =>
            {
                using var aes = Aes.Create();
                aes.Key = _masterKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Combine IV + cipher
                var combined = new byte[aes.IV.Length + cipherBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

                result.EncryptedText = Convert.ToBase64String(combined);
                result.IsSuccess = true;

            }, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error encrypting bytes");
            return result;
        }
    }

    /// <summary>
    /// Decrypts data using AES-256-CBC
    /// </summary>
    public async Task<DecryptionBytesResult> DecryptBytesAsync(
        string encryptedText,
        CancellationToken cancellationToken = default)
    {
        var result = new DecryptionBytesResult();

        try
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Encrypted text cannot be null or empty";
                return result;
            }

            _logger.LogDebug("Decrypting bytes");

            await Task.Run(() =>
            {
                var combined = Convert.FromBase64String(encryptedText);

                using var aes = Aes.Create();
                aes.Key = _masterKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV
                var iv = new byte[16];
                var cipherBytes = new byte[combined.Length - 16];
                Buffer.BlockCopy(combined, 0, iv, 0, 16);
                Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                result.DecryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                result.IsSuccess = true;

            }, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error decrypting bytes");
            return result;
        }
    }
}

/// <summary>
/// Interface for encryption service
/// </summary>
public interface IEncryptionService
{
    Task<EncryptionResult> EncryptStringAsync(string plainText, CancellationToken cancellationToken = default);
    Task<DecryptionResult> DecryptStringAsync(string encryptedText, CancellationToken cancellationToken = default);
    Task<string> GenerateKeyAsync(CancellationToken cancellationToken = default);
    Task<PasswordHashResult> HashPasswordAsync(string password, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default);
    Task<EncryptionResult> EncryptBytesAsync(byte[] plainBytes, CancellationToken cancellationToken = default);
    Task<DecryptionBytesResult> DecryptBytesAsync(string encryptedText, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of encryption operation
/// </summary>
public class EncryptionResult
{
    public bool IsSuccess { get; set; }
    public string EncryptedText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of decryption operation
/// </summary>
public class DecryptionResult
{
    public bool IsSuccess { get; set; }
    public string DecryptedText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of decryption bytes operation
/// </summary>
public class DecryptionBytesResult
{
    public bool IsSuccess { get; set; }
    public byte[] DecryptedBytes { get; set; } = Array.Empty<byte>();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of password hashing operation
/// </summary>
public class PasswordHashResult
{
    public bool IsSuccess { get; set; }
    public string HashedPassword { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
