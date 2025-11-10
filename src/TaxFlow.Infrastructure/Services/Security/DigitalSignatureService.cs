using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Security;

/// <summary>
/// Digital signature service implementation using CADES-BES format for ETA compliance
/// </summary>
public class DigitalSignatureService : IDigitalSignatureService
{
    private readonly ICertificateService _certificateService;
    private readonly ILogger<DigitalSignatureService> _logger;

    public DigitalSignatureService(
        ICertificateService certificateService,
        ILogger<DigitalSignatureService> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    /// <summary>
    /// Signs a JSON document using CADES-BES format
    /// </summary>
    public async Task<SignatureResult> SignJsonDocumentAsync(
        string jsonContent,
        string certificateThumbprint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get certificate
            var certificate = await _certificateService.GetCertificateAsync(certificateThumbprint, cancellationToken);
            if (certificate == null)
            {
                return new SignatureResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Certificate with thumbprint {certificateThumbprint} not found"
                };
            }

            // Validate certificate
            var validation = await _certificateService.ValidateCertificateAsync(certificateThumbprint, cancellationToken);
            if (!validation.IsValid)
            {
                return new SignatureResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Certificate validation failed: {string.Join(", ", validation.Errors)}"
                };
            }

            // Convert JSON to bytes
            var contentBytes = Encoding.UTF8.GetBytes(jsonContent);

            // Create ContentInfo
            var contentInfo = new ContentInfo(contentBytes);

            // Create SignedCms object
            var signedCms = new SignedCms(contentInfo, true); // true = detached signature

            // Create CmsSigner with the certificate
            var cmsSigner = new CmsSigner(certificate)
            {
                IncludeOption = X509IncludeOption.WholeChain,
                DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1") // SHA-256
            };

            // Add signing time attribute (required for CADES-BES)
            var signingTime = new Pkcs9SigningTime(DateTime.UtcNow);
            cmsSigner.SignedAttributes.Add(signingTime);

            // Sign the content
            signedCms.ComputeSignature(cmsSigner);

            // Get the signature bytes
            var signatureBytes = signedCms.Encode();

            // Convert to Base64
            var signatureValue = Convert.ToBase64String(signatureBytes);

            _logger.LogInformation(
                "Successfully signed document using certificate {SerialNumber}",
                certificate.SerialNumber);

            return new SignatureResult
            {
                IsSuccess = true,
                SignedContent = jsonContent,
                SignatureValue = signatureValue,
                SignedAt = DateTime.UtcNow,
                CertificateSerialNumber = certificate.SerialNumber
            };
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error while signing document");
            return new SignatureResult
            {
                IsSuccess = false,
                ErrorMessage = $"Cryptographic error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing document");
            return new SignatureResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Signs multiple documents in batch
    /// </summary>
    public async Task<List<SignatureResult>> SignBatchAsync(
        List<string> jsonDocuments,
        string certificateThumbprint,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SignatureResult>();

        // Validate certificate once for the batch
        var certificate = await _certificateService.GetCertificateAsync(certificateThumbprint, cancellationToken);
        if (certificate == null)
        {
            _logger.LogError("Certificate {Thumbprint} not found for batch signing", certificateThumbprint);
            return jsonDocuments.Select(_ => new SignatureResult
            {
                IsSuccess = false,
                ErrorMessage = "Certificate not found"
            }).ToList();
        }

        // Sign each document
        foreach (var document in jsonDocuments)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await SignJsonDocumentAsync(document, certificateThumbprint, cancellationToken);
            results.Add(result);
        }

        _logger.LogInformation(
            "Batch signing completed: {Success}/{Total} documents signed successfully",
            results.Count(r => r.IsSuccess),
            jsonDocuments.Count);

        return results;
    }

    /// <summary>
    /// Verifies a digital signature
    /// </summary>
    public async Task<bool> VerifySignatureAsync(
        string signedContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var signatureBytes = Convert.FromBase64String(signedContent);
            var signedCms = new SignedCms();
            signedCms.Decode(signatureBytes);

            // Verify the signature
            signedCms.CheckSignature(true);

            _logger.LogInformation("Signature verification successful");
            return true;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Signature verification failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature");
            return false;
        }
    }

    /// <summary>
    /// Gets signature information
    /// </summary>
    public async Task<SignatureInfo?> GetSignatureInfoAsync(
        string signedContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var signatureBytes = Convert.FromBase64String(signedContent);
            var signedCms = new SignedCms();
            signedCms.Decode(signatureBytes);

            var signerInfo = signedCms.SignerInfos[0];
            var certificate = signerInfo.Certificate;

            // Get signing time
            var signingTime = DateTime.UtcNow;
            foreach (var attr in signerInfo.SignedAttributes)
            {
                if (attr.Oid?.Value == "1.2.840.113549.1.9.5") // Signing time OID
                {
                    var pkcs9SigningTime = new Pkcs9SigningTime(attr.Values[0].RawData);
                    signingTime = pkcs9SigningTime.SigningTime.ToUniversalTime();
                    break;
                }
            }

            // Verify signature
            var isValid = true;
            var errors = new List<string>();
            try
            {
                signedCms.CheckSignature(true);
            }
            catch (CryptographicException ex)
            {
                isValid = false;
                errors.Add(ex.Message);
            }

            return new SignatureInfo
            {
                SignatureAlgorithm = signerInfo.DigestAlgorithm.FriendlyName ?? "Unknown",
                SignedAt = signingTime,
                SignerName = certificate?.Subject ?? "Unknown",
                CertificateSerialNumber = certificate?.SerialNumber ?? "Unknown",
                IsValid = isValid,
                ValidationErrors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature info");
            return null;
        }
    }
}
