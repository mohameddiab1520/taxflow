namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for digital signature service (CADES-BES format for ETA compliance)
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>
    /// Signs a JSON document using CADES-BES format
    /// </summary>
    /// <param name="jsonContent">The JSON document to sign</param>
    /// <param name="certificateThumbprint">The certificate thumbprint to use for signing</param>
    Task<SignatureResult> SignJsonDocumentAsync(string jsonContent, string certificateThumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs multiple documents in batch
    /// </summary>
    Task<List<SignatureResult>> SignBatchAsync(List<string> jsonDocuments, string certificateThumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a digital signature
    /// </summary>
    Task<bool> VerifySignatureAsync(string signedContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets signature information
    /// </summary>
    Task<SignatureInfo?> GetSignatureInfoAsync(string signedContent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of digital signature operation
/// </summary>
public class SignatureResult
{
    public bool IsSuccess { get; set; }
    public string? SignedContent { get; set; }
    public string? SignatureValue { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SignedAt { get; set; }
    public string? CertificateSerialNumber { get; set; }
}

/// <summary>
/// Information about a digital signature
/// </summary>
public class SignatureInfo
{
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}
