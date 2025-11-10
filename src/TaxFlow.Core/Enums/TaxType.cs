namespace TaxFlow.Core.Enums;

/// <summary>
/// Types of taxes according to ETA standards
/// </summary>
public enum TaxType
{
    /// <summary>
    /// Value Added Tax (T1)
    /// </summary>
    VAT = 1,

    /// <summary>
    /// Table Tax (T2)
    /// </summary>
    TableTax = 2,

    /// <summary>
    /// Entertainment Tax (T3)
    /// </summary>
    EntertainmentTax = 3,

    /// <summary>
    /// Tobacco Tax (T4)
    /// </summary>
    TobaccoTax = 4,

    /// <summary>
    /// Customs Duties (T5)
    /// </summary>
    CustomsDuties = 5,

    /// <summary>
    /// Development Fee (T6)
    /// </summary>
    DevelopmentFee = 6,

    /// <summary>
    /// Stamp Duty (T7)
    /// </summary>
    StampDuty = 7,

    /// <summary>
    /// Withholding Tax (T8)
    /// </summary>
    WithholdingTax = 8
}
