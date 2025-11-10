namespace TaxFlow.Core.Interfaces;

/// <summary>
/// Interface for advanced reporting service
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generates an invoice PDF report
    /// </summary>
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a receipt PDF report
    /// </summary>
    Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports invoices to Excel
    /// </summary>
    Task<byte[]> ExportInvoicesToExcelAsync(InvoiceExportOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports receipts to Excel
    /// </summary>
    Task<byte[]> ExportReceiptsToExcelAsync(ReceiptExportOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a tax summary report
    /// </summary>
    Task<byte[]> GenerateTaxSummaryReportAsync(TaxSummaryOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sales analysis report
    /// </summary>
    Task<byte[]> GenerateSalesAnalysisReportAsync(SalesAnalysisOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// Invoice export options
/// </summary>
public class InvoiceExportOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public bool IncludeLines { get; set; } = true;
    public bool IncludeTaxDetails { get; set; } = true;
}

/// <summary>
/// Receipt export options
/// </summary>
public class ReceiptExportOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Terminal { get; set; }
    public bool IncludeLines { get; set; } = true;
}

/// <summary>
/// Tax summary report options
/// </summary>
public class TaxSummaryOptions
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool GroupByTaxType { get; set; } = true;
    public bool IncludeZeroRated { get; set; } = true;
    public ReportFormat Format { get; set; } = ReportFormat.Pdf;
}

/// <summary>
/// Sales analysis report options
/// </summary>
public class SalesAnalysisOptions
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SalesGrouping GroupBy { get; set; } = SalesGrouping.Daily;
    public bool IncludeCharts { get; set; } = true;
    public ReportFormat Format { get; set; } = ReportFormat.Pdf;
}

/// <summary>
/// Report output formats
/// </summary>
public enum ReportFormat
{
    Pdf,
    Excel,
    Csv
}

/// <summary>
/// Sales grouping options
/// </summary>
public enum SalesGrouping
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}
