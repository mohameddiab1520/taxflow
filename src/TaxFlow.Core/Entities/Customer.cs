using System.ComponentModel.DataAnnotations;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Core.Entities;

/// <summary>
/// Customer entity representing invoice recipient/issuer
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Customer name (Arabic)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Customer name (English)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Tax registration number (TIN)
    /// </summary>
    [StringLength(50)]
    public string? TaxRegistrationNumber { get; set; }

    /// <summary>
    /// Commercial registration number
    /// </summary>
    [StringLength(50)]
    public string? CommercialRegistrationNumber { get; set; }

    /// <summary>
    /// National ID for individuals
    /// </summary>
    [StringLength(50)]
    public string? NationalId { get; set; }

    /// <summary>
    /// Customer type (B - Business, P - Person, F - Foreign)
    /// </summary>
    [Required]
    [StringLength(1)]
    public string CustomerType { get; set; } = "B";

    /// <summary>
    /// Customer address
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Contact email
    /// </summary>
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone
    /// </summary>
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Is this customer tax-exempt
    /// </summary>
    public bool IsTaxExempt { get; set; } = false;

    /// <summary>
    /// Additional notes
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Collection of invoices for this customer
    /// </summary>
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    /// <summary>
    /// Collection of receipts for this customer
    /// </summary>
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
