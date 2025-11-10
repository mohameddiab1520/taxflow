namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Service for AES-256 encryption and secure password hashing
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a string using AES-256-GCM
    /// </summary>
    Task<string> EncryptStringAsync(string plainText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a string using AES-256-GCM
    /// </summary>
    Task<string> DecryptStringAsync(string cipherText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new 256-bit encryption key
    /// </summary>
    Task<string> GenerateKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Hashes a password using PBKDF2 with SHA-256
    /// </summary>
    Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    Task<bool> VerifyPasswordAsync(string password, string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts binary data
    /// </summary>
    Task<byte[]> EncryptBytesAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts binary data
    /// </summary>
    Task<byte[]> DecryptBytesAsync(byte[] encryptedData, CancellationToken cancellationToken = default);
}
