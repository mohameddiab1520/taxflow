namespace TaxFlow.Core.Exceptions;

/// <summary>
/// Exception thrown when document signing fails
/// </summary>
public class DocumentSigningException : Exception
{
    public string? DocumentId { get; }
    public string? CertificateThumbprint { get; }

    public DocumentSigningException(string message) : base(message)
    {
    }

    public DocumentSigningException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DocumentSigningException(string message, string documentId, string certificateThumbprint)
        : base(message)
    {
        DocumentId = documentId;
        CertificateThumbprint = certificateThumbprint;
    }
}
