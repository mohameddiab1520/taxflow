using System.ComponentModel.DataAnnotations;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Core.Entities;

/// <summary>
/// Receipt line item entity for B2C transactions
/// </summary>
public class ReceiptLine : BaseEntity
{
    /// <summary>
    /// Reference to parent receipt
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Navigation property to parent receipt
    /// </summary>
    public Receipt? Receipt { get; set; }

    /// <summary>
    /// Line number/sequence
    /// </summary>
    [Required]
    public int LineNumber { get; set; }

    /// <summary>
    /// Item/Service description (Arabic)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string DescriptionAr { get; set; } = string.Empty;

    /// <summary>
    /// Item/Service description (English)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string DescriptionEn { get; set; } = string.Empty;

    /// <summary>
    /// Item code/SKU
    /// </summary>
    [StringLength(100)]
    public string? ItemCode { get; set; }

    /// <summary>
    /// Unit type (e.g., EA, KG, L, etc.)
    /// </summary>
    [Required]
    [StringLength(10)]
    public string UnitType { get; set; } = "EA";

    /// <summary>
    /// Quantity
    /// </summary>
    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price (excluding tax)
    /// </summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount amount
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; } = 0;

    /// <summary>
    /// Net amount (after discount, before tax)
    /// </summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Total amount (including tax)
    /// </summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Collection of tax items for this line
    /// </summary>
    public List<TaxItem> TaxItems { get; set; } = new();

    /// <summary>
    /// Total tax amount
    /// </summary>
    public decimal TotalTaxAmount { get; set; }

    /// <summary>
    /// Calculates line totals
    /// </summary>
    public void CalculateTotals()
    {
        // Calculate net amount
        var grossAmount = Quantity * UnitPrice;
        NetAmount = Math.Round(grossAmount - Discount, 5);

        // Calculate tax for each tax item
        foreach (var taxItem in TaxItems)
        {
            taxItem.Amount = NetAmount;
            taxItem.CalculateTaxValue();
        }

        // Calculate total tax
        TotalTaxAmount = Math.Round(TaxItems.Sum(t => t.TaxValue), 5);

        // Calculate total amount
        TotalAmount = Math.Round(NetAmount + TotalTaxAmount, 5);
    }
}
