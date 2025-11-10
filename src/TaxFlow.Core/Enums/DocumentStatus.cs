namespace TaxFlow.Core.Enums;

/// <summary>
/// Status of document in the ETA submission workflow
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// Document created but not yet validated
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Document is being validated
    /// </summary>
    Validating = 2,

    /// <summary>
    /// Document passed validation
    /// </summary>
    Valid = 3,

    /// <summary>
    /// Document failed validation
    /// </summary>
    Invalid = 4,

    /// <summary>
    /// Document is being signed
    /// </summary>
    Signing = 5,

    /// <summary>
    /// Document is being submitted to ETA
    /// </summary>
    Submitting = 6,

    /// <summary>
    /// Document successfully submitted to ETA
    /// </summary>
    Submitted = 7,

    /// <summary>
    /// Document accepted by ETA
    /// </summary>
    Accepted = 8,

    /// <summary>
    /// Document rejected by ETA
    /// </summary>
    Rejected = 9,

    /// <summary>
    /// Document cancelled
    /// </summary>
    Cancelled = 10,

    /// <summary>
    /// Submission failed due to error
    /// </summary>
    Failed = 11
}
