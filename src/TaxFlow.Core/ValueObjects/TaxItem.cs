using System.ComponentModel.DataAnnotations;
using TaxFlow.Core.Enums;

namespace TaxFlow.Core.ValueObjects;

/// <summary>
/// Value object representing a tax line item
/// </summary>
public class TaxItem
{
    /// <summary>
    /// Type of tax
    /// </summary>
    [Required]
    public TaxType TaxType { get; set; }

    /// <summary>
    /// Tax rate (percentage)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public decimal Rate { get; set; }

    /// <summary>
    /// Taxable amount
    /// </summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Calculated tax value
    /// </summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal TaxValue { get; set; }

    /// <summary>
    /// Tax sub-type (optional - for specific tax categories)
    /// </summary>
    [StringLength(10)]
    public string? SubType { get; set; }

    /// <summary>
    /// Calculates the tax value based on amount and rate
    /// </summary>
    public void CalculateTaxValue()
    {
        TaxValue = Math.Round(Amount * (Rate / 100), 5);
    }
}
