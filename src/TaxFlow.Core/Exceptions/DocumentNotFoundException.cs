namespace TaxFlow.Core.Exceptions;

/// <summary>
/// Exception thrown when a document is not found
/// </summary>
public class DocumentNotFoundException : Exception
{
    public string? DocumentId { get; }
    public string? DocumentType { get; }

    public DocumentNotFoundException(string message) : base(message)
    {
    }

    public DocumentNotFoundException(string message, string documentId, string documentType)
        : base(message)
    {
        DocumentId = documentId;
        DocumentType = documentType;
    }
}
