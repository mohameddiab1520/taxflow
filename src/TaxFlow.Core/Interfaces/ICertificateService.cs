using System.Security.Cryptography.X509Certificates;

namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for certificate management service
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Gets all available certificates from the Windows certificate store
    /// </summary>
    Task<List<CertificateInfo>> GetAvailableCertificatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a certificate by thumbprint
    /// </summary>
    Task<X509Certificate2?> GetCertificateAsync(string thumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a certificate for ETA usage
    /// </summary>
    Task<CertificateValidationResult> ValidateCertificateAsync(string thumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a certificate from a PFX file
    /// </summary>
    Task<bool> InstallCertificateAsync(string pfxPath, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a certificate to PFX file
    /// </summary>
    Task<bool> ExportCertificateAsync(string thumbprint, string outputPath, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a certificate from the store
    /// </summary>
    Task<bool> RemoveCertificateAsync(string thumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a certificate is expiring soon (within specified days)
    /// </summary>
    Task<bool> IsCertificateExpiringSoonAsync(string thumbprint, int daysThreshold = 30, CancellationToken cancellationToken = default);
}

/// <summary>
/// Certificate information
/// </summary>
public class CertificateInfo
{
    public string Thumbprint { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool HasPrivateKey { get; set; }
    public string FriendlyName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public int DaysUntilExpiry => (ValidTo - DateTime.Now).Days;
    public bool IsExpired => DateTime.Now > ValidTo;
    public bool IsValid => !IsExpired && HasPrivateKey;
}

/// <summary>
/// Certificate validation result
/// </summary>
public class CertificateValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public CertificateInfo? CertificateInfo { get; set; }
}
