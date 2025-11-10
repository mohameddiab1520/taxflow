using System.ComponentModel.DataAnnotations;
using TaxFlow.Core.Enums;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Core.Entities;

/// <summary>
/// Receipt entity representing a B2C POS transaction
/// </summary>
public class Receipt : BaseEntity
{
    /// <summary>
    /// Internal receipt number
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Issue date and time
    /// </summary>
    [Required]
    public DateTime DateTimeIssued { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the document
    /// </summary>
    [Required]
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Customer ID reference (optional for B2C)
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Customer navigation property
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// POS terminal/branch identifier
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TerminalId { get; set; } = string.Empty;

    /// <summary>
    /// Cashier/operator identifier
    /// </summary>
    [StringLength(50)]
    public string? CashierId { get; set; }

    /// <summary>
    /// Collection of receipt lines
    /// </summary>
    public List<ReceiptLine> Lines { get; set; } = new();

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
    /// Payment method (Cash, Card, Mobile, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>
    /// Amount tendered (for cash payments)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? AmountTendered { get; set; }

    /// <summary>
    /// Change returned (for cash payments)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? ChangeReturned { get; set; }

    /// <summary>
    /// Transaction reference (for card/mobile payments)
    /// </summary>
    [StringLength(100)]
    public string? TransactionReference { get; set; }

    /// <summary>
    /// ETA submission reference
    /// </summary>
    [StringLength(200)]
    public string? EtaSubmissionReference { get; set; }

    /// <summary>
    /// ETA acknowledgement ID
    /// </summary>
    [StringLength(200)]
    public string? EtaAcknowledgementId { get; set; }

    /// <summary>
    /// ETA response date
    /// </summary>
    public DateTime? EtaResponseDate { get; set; }

    /// <summary>
    /// Validation errors (if any)
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// JSON representation of the document for ETA submission
    /// </summary>
    public string? JsonDocument { get; set; }

    /// <summary>
    /// Notes/Comments
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Calculates all totals for the receipt
    /// </summary>
    public void CalculateTotals()
    {
        // Calculate line totals first
        foreach (var line in Lines)
        {
            line.CalculateTotals();
        }

        // Calculate receipt totals
        TotalSalesAmount = Math.Round(Lines.Sum(l => l.Quantity * l.UnitPrice), 5);
        TotalDiscountAmount = Math.Round(Lines.Sum(l => l.Discount), 5);
        NetAmount = Math.Round(Lines.Sum(l => l.NetAmount), 5);
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

        // Calculate change if cash payment
        if (PaymentMethod == "Cash" && AmountTendered.HasValue)
        {
            ChangeReturned = Math.Round(AmountTendered.Value - TotalAmount, 2);
        }
    }
}
