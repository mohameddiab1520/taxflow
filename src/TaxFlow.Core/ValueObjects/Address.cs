using System.ComponentModel.DataAnnotations;

namespace TaxFlow.Core.ValueObjects;

/// <summary>
/// Value object representing an address according to ETA standards
/// </summary>
public class Address
{
    /// <summary>
    /// Country code (e.g., EG for Egypt)
    /// </summary>
    [Required]
    [StringLength(2)]
    public string Country { get; set; } = "EG";

    /// <summary>
    /// Governorate/Province
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Governate { get; set; } = string.Empty;

    /// <summary>
    /// Region/District
    /// </summary>
    [StringLength(100)]
    public string? RegionCity { get; set; }

    /// <summary>
    /// Street name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Building number
    /// </summary>
    [Required]
    [StringLength(50)]
    public string BuildingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Postal code
    /// </summary>
    [StringLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Floor number
    /// </summary>
    [StringLength(10)]
    public string? Floor { get; set; }

    /// <summary>
    /// Room/Apartment number
    /// </summary>
    [StringLength(10)]
    public string? Room { get; set; }

    /// <summary>
    /// Landmark reference
    /// </summary>
    [StringLength(200)]
    public string? Landmark { get; set; }

    /// <summary>
    /// Additional information
    /// </summary>
    [StringLength(500)]
    public string? AdditionalInformation { get; set; }
}
