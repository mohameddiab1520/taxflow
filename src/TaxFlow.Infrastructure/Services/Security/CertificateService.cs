using System.Security.Cryptography.X509Certificates;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Security;

/// <summary>
/// Certificate management service for Windows certificate store
/// </summary>
public class CertificateService : ICertificateService
{
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(ILogger<CertificateService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all available certificates from the Windows certificate store
    /// </summary>
    public Task<List<CertificateInfo>> GetAvailableCertificatesAsync(
        CancellationToken cancellationToken = default)
    {
        var certificates = new List<CertificateInfo>();

        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                certificates.Add(new CertificateInfo
                {
                    Thumbprint = cert.Thumbprint,
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    ValidFrom = cert.NotBefore,
                    ValidTo = cert.NotAfter,
                    HasPrivateKey = cert.HasPrivateKey,
                    FriendlyName = cert.FriendlyName ?? string.Empty,
                    SerialNumber = cert.SerialNumber
                });
            }

            store.Close();

            _logger.LogInformation("Found {Count} certificates in store", certificates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading certificates from store");
        }

        return Task.FromResult(certificates);
    }

    /// <summary>
    /// Gets a certificate by thumbprint
    /// </summary>
    public Task<X509Certificate2?> GetCertificateAsync(
        string thumbprint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: false);

            store.Close();

            if (certificates.Count > 0)
            {
                _logger.LogInformation("Certificate found: {Subject}", certificates[0].Subject);
                return Task.FromResult<X509Certificate2?>(certificates[0]);
            }

            _logger.LogWarning("Certificate with thumbprint {Thumbprint} not found", thumbprint);
            return Task.FromResult<X509Certificate2?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate {Thumbprint}", thumbprint);
            return Task.FromResult<X509Certificate2?>(null);
        }
    }

    /// <summary>
    /// Validates a certificate for ETA usage
    /// </summary>
    public async Task<CertificateValidationResult> ValidateCertificateAsync(
        string thumbprint,
        CancellationToken cancellationToken = default)
    {
        var result = new CertificateValidationResult();

        try
        {
            var certificate = await GetCertificateAsync(thumbprint, cancellationToken);
            if (certificate == null)
            {
                result.IsValid = false;
                result.Errors.Add("Certificate not found");
                return result;
            }

            var certInfo = new CertificateInfo
            {
                Thumbprint = certificate.Thumbprint,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                ValidFrom = certificate.NotBefore,
                ValidTo = certificate.NotAfter,
                HasPrivateKey = certificate.HasPrivateKey,
                FriendlyName = certificate.FriendlyName ?? string.Empty,
                SerialNumber = certificate.SerialNumber
            };

            result.CertificateInfo = certInfo;

            // Check if certificate is expired
            if (DateTime.Now > certificate.NotAfter)
            {
                result.IsValid = false;
                result.Errors.Add($"Certificate expired on {certificate.NotAfter:yyyy-MM-dd}");
            }

            // Check if certificate is not yet valid
            if (DateTime.Now < certificate.NotBefore)
            {
                result.IsValid = false;
                result.Errors.Add($"Certificate not valid until {certificate.NotBefore:yyyy-MM-dd}");
            }

            // Check if certificate has private key
            if (!certificate.HasPrivateKey)
            {
                result.IsValid = false;
                result.Errors.Add("Certificate does not have a private key");
            }

            // Check if certificate is expiring soon (within 30 days)
            var daysUntilExpiry = (certificate.NotAfter - DateTime.Now).Days;
            if (daysUntilExpiry > 0 && daysUntilExpiry <= 30)
            {
                result.Warnings.Add($"Certificate expires in {daysUntilExpiry} days");
            }

            // Verify certificate chain
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            var chainBuilt = chain.Build(certificate);
            if (!chainBuilt)
            {
                result.Warnings.Add("Certificate chain validation failed");
                foreach (var status in chain.ChainStatus)
                {
                    result.Warnings.Add($"Chain status: {status.StatusInformation}");
                }
            }

            // If no errors, mark as valid
            if (result.Errors.Count == 0)
            {
                result.IsValid = true;
                _logger.LogInformation(
                    "Certificate {Subject} validated successfully",
                    certificate.Subject);
            }
            else
            {
                _logger.LogWarning(
                    "Certificate {Subject} validation failed: {Errors}",
                    certificate.Subject,
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate {Thumbprint}", thumbprint);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Installs a certificate from a PFX file
    /// </summary>
    public Task<bool> InstallCertificateAsync(
        string pfxPath,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.PersistKeySet);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();

            _logger.LogInformation(
                "Successfully installed certificate {Subject} with thumbprint {Thumbprint}",
                certificate.Subject,
                certificate.Thumbprint);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing certificate from {Path}", pfxPath);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Exports a certificate to PFX file
    /// </summary>
    public async Task<bool> ExportCertificateAsync(
        string thumbprint,
        string outputPath,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await GetCertificateAsync(thumbprint, cancellationToken);
            if (certificate == null)
            {
                _logger.LogError("Certificate {Thumbprint} not found for export", thumbprint);
                return false;
            }

            var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
            await File.WriteAllBytesAsync(outputPath, pfxBytes, cancellationToken);

            _logger.LogInformation(
                "Successfully exported certificate {Subject} to {Path}",
                certificate.Subject,
                outputPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting certificate {Thumbprint}", thumbprint);
            return false;
        }
    }

    /// <summary>
    /// Removes a certificate from the store
    /// </summary>
    public async Task<bool> RemoveCertificateAsync(
        string thumbprint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await GetCertificateAsync(thumbprint, cancellationToken);
            if (certificate == null)
            {
                _logger.LogError("Certificate {Thumbprint} not found for removal", thumbprint);
                return false;
            }

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Remove(certificate);
            store.Close();

            _logger.LogInformation(
                "Successfully removed certificate {Subject}",
                certificate.Subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing certificate {Thumbprint}", thumbprint);
            return false;
        }
    }

    /// <summary>
    /// Checks if a certificate is expiring soon (within specified days)
    /// </summary>
    public async Task<bool> IsCertificateExpiringSoonAsync(
        string thumbprint,
        int daysThreshold = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await GetCertificateAsync(thumbprint, cancellationToken);
            if (certificate == null)
                return false;

            var daysUntilExpiry = (certificate.NotAfter - DateTime.Now).Days;
            return daysUntilExpiry >= 0 && daysUntilExpiry <= daysThreshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking certificate expiry {Thumbprint}", thumbprint);
            return false;
        }
    }
}
