namespace TaxFlow.Core.Exceptions;

/// <summary>
/// Exception thrown when ETA submission fails
/// </summary>
public class EtaSubmissionException : Exception
{
    public string? DocumentId { get; }
    public string? EtaResponse { get; }
    public int? StatusCode { get; }

    public EtaSubmissionException(string message) : base(message)
    {
    }

    public EtaSubmissionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EtaSubmissionException(string message, string documentId, string? etaResponse = null, int? statusCode = null)
        : base(message)
    {
        DocumentId = documentId;
        EtaResponse = etaResponse;
        StatusCode = statusCode;
    }
}
