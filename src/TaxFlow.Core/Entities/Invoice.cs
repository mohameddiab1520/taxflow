using System.ComponentModel.DataAnnotations;
using TaxFlow.Core.Enums;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Core.Entities;

/// <summary>
/// Invoice entity representing a B2B tax document
/// </summary>
public class Invoice : BaseEntity
{
    /// <summary>
    /// Internal invoice number
    /// </summary>
    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Document type (Invoice, Credit Note, Debit Note)
    /// </summary>
    [Required]
    public DocumentType DocumentType { get; set; } = DocumentType.Invoice;

    /// <summary>
    /// Document type version (e.g., "1.0")
    /// </summary>
    [Required]
    [StringLength(10)]
    public string DocumentTypeVersion { get; set; } = "1.0";

    /// <summary>
    /// Issue date and time
    /// </summary>
    [Required]
    public DateTime DateTimeIssued { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tax point date (date when tax obligation occurs)
    /// </summary>
    public DateTime? TaxpointDate { get; set; }

    /// <summary>
    /// Current status of the document
    /// </summary>
    [Required]
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Customer ID reference
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Customer navigation property
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Issuer information (the company issuing the invoice)
    /// </summary>
    public Customer? Issuer { get; set; }

    /// <summary>
    /// Collection of invoice lines
    /// </summary>
    public List<InvoiceLine> Lines { get; set; } = new();

    /// <summary>
    /// Total sales amount (sum of all lines before discount)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal TotalSalesAmount { get; set; }

    /// <summary>
    /// Total discount amount
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal TotalDiscountAmount { get; set; }

    /// <summary>
    /// Net amount (after discount, before tax)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Total tax amount
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal TotalTaxAmount { get; set; }

    /// <summary>
    /// Total amount payable (including all taxes)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Aggregated tax totals by tax type
    /// </summary>
    public List<TaxItem> TaxTotals { get; set; } = new();

    /// <summary>
    /// Extra discount amount at document level
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal ExtraDiscountAmount { get; set; } = 0;

    /// <summary>
    /// Total amount in words (Arabic)
    /// </summary>
    [StringLength(500)]
    public string? TotalAmountInWordsAr { get; set; }

    /// <summary>
    /// Total amount in words (English)
    /// </summary>
    [StringLength(500)]
    public string? TotalAmountInWordsEn { get; set; }

    /// <summary>
    /// Purchase order reference
    /// </summary>
    [StringLength(50)]
    public string? PurchaseOrderReference { get; set; }

    /// <summary>
    /// Sales order reference
    /// </summary>
    [StringLength(50)]
    public string? SalesOrderReference { get; set; }

    /// <summary>
    /// Proforma invoice number (if applicable)
    /// </summary>
    [StringLength(50)]
    public string? ProformaInvoiceNumber { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Delivery date
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// ETA submission unique ID (UUID)
    /// </summary>
    public Guid? EtaSubmissionId { get; set; }

    /// <summary>
    /// ETA response - Long ID
    /// </summary>
    [StringLength(200)]
    public string? EtaLongId { get; set; }

    /// <summary>
    /// ETA response - Internal ID
    /// </summary>
    [StringLength(200)]
    public string? EtaInternalId { get; set; }

    /// <summary>
    /// ETA acceptance/rejection date
    /// </summary>
    public DateTime? EtaResponseDate { get; set; }

    /// <summary>
    /// Digital signature (CADES-BES format)
    /// </summary>
    public string? DigitalSignature { get; set; }

    /// <summary>
    /// Validation errors (if any)
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// ETA rejection reasons (if rejected)
    /// </summary>
    public string? RejectionReasons { get; set; }

    /// <summary>
    /// JSON representation of the document for ETA submission
    /// </summary>
    public string? JsonDocument { get; set; }

    /// <summary>
    /// Notes/Comments
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Calculates all totals for the invoice
    /// </summary>
    public void CalculateTotals()
    {
        // Calculate line totals first
        foreach (var line in Lines)
        {
            line.CalculateTotals();
        }

        // Calculate invoice totals
        TotalSalesAmount = Math.Round(Lines.Sum(l => l.Quantity * l.UnitPrice), 5);
        TotalDiscountAmount = Math.Round(Lines.Sum(l => l.Discount) + ExtraDiscountAmount, 5);
        NetAmount = Math.Round(Lines.Sum(l => l.NetAmount) - ExtraDiscountAmount, 5);
        TotalTaxAmount = Math.Round(Lines.Sum(l => l.TotalTaxAmount), 5);
        TotalAmount = Math.Round(NetAmount + TotalTaxAmount, 5);

        // Aggregate tax totals by tax type
        TaxTotals = Lines
            .SelectMany(l => l.TaxItems)
            .GroupBy(t => t.TaxType)
            .Select(g => new TaxItem
            {
                TaxType = g.Key,
                Rate = g.First().Rate,
                Amount = Math.Round(g.Sum(t => t.Amount), 5),
                TaxValue = Math.Round(g.Sum(t => t.TaxValue), 5)
            })
            .ToList();
    }
}
