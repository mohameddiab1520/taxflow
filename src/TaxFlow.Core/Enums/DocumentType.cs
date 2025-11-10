namespace TaxFlow.Core.Enums;

/// <summary>
/// Type of tax document according to ETA standards
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Invoice (I) - Standard B2B invoice
    /// </summary>
    Invoice = 1,

    /// <summary>
    /// Credit Note (C) - Credit adjustment
    /// </summary>
    CreditNote = 2,

    /// <summary>
    /// Debit Note (D) - Debit adjustment
    /// </summary>
    DebitNote = 3,

    /// <summary>
    /// Receipt (R) - B2C POS receipt
    /// </summary>
    Receipt = 4
}
