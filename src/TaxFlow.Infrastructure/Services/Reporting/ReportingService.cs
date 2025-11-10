using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace TaxFlow.Infrastructure.Services.Reporting;

/// <summary>
/// Advanced reporting service for PDF and Excel generation
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        IUnitOfWork unitOfWork,
        ILogger<ReportingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _unitOfWork.Invoices.GetWithDetailsAsync(invoiceId);
            if (invoice == null)
                throw new Exception("Invoice not found");

            _logger.LogInformation("Generating PDF for invoice {InvoiceNumber}", invoice.InvoiceNumber);

            // TODO: Implement actual PDF generation using library like QuestPDF or iTextSharp
            // For now, return empty byte array
            var pdfContent = GenerateInvoicePdfContent(invoice);
            return Encoding.UTF8.GetBytes(pdfContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF for {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var receipt = await _unitOfWork.Receipts.GetWithDetailsAsync(receiptId);
            if (receipt == null)
                throw new Exception("Receipt not found");

            _logger.LogInformation("Generating PDF for receipt {ReceiptNumber}", receipt.ReceiptNumber);

            var pdfContent = GenerateReceiptPdfContent(receipt);
            return Encoding.UTF8.GetBytes(pdfContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt PDF for {ReceiptId}", receiptId);
            throw;
        }
    }

    public async Task<byte[]> ExportInvoicesToExcelAsync(
        InvoiceExportOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Exporting invoices to Excel from {StartDate} to {EndDate}",
                options.StartDate,
                options.EndDate);

            var invoices = await _unitOfWork.Invoices.GetAllAsync();

            // Apply filters
            if (options.StartDate.HasValue)
                invoices = invoices.Where(i => i.DateTimeIssued >= options.StartDate.Value);

            if (options.EndDate.HasValue)
                invoices = invoices.Where(i => i.DateTimeIssued <= options.EndDate.Value);

            if (!string.IsNullOrEmpty(options.Status))
                invoices = invoices.Where(i => i.Status.ToString() == options.Status);

            if (options.CustomerId.HasValue)
                invoices = invoices.Where(i => i.CustomerId == options.CustomerId.Value);

            // TODO: Implement actual Excel generation using ClosedXML or EPPlus
            // For now, generate CSV content
            var csvContent = GenerateInvoicesCsv(invoices.ToList(), options);
            return Encoding.UTF8.GetBytes(csvContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportReceiptsToExcelAsync(
        ReceiptExportOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Exporting receipts to Excel from {StartDate} to {EndDate}",
                options.StartDate,
                options.EndDate);

            var receipts = await _unitOfWork.Receipts.GetAllAsync();

            // Apply filters
            if (options.StartDate.HasValue)
                receipts = receipts.Where(r => r.DateTimeIssued >= options.StartDate.Value);

            if (options.EndDate.HasValue)
                receipts = receipts.Where(r => r.DateTimeIssued <= options.EndDate.Value);

            if (!string.IsNullOrEmpty(options.PaymentMethod))
                receipts = receipts.Where(r => r.PaymentMethod == options.PaymentMethod);

            var csvContent = GenerateReceiptsCsv(receipts.ToList(), options);
            return Encoding.UTF8.GetBytes(csvContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting receipts to Excel");
            throw;
        }
    }

    public async Task<byte[]> GenerateTaxSummaryReportAsync(
        TaxSummaryOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating tax summary report from {StartDate} to {EndDate}",
                options.StartDate,
                options.EndDate);

            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var filteredInvoices = invoices
                .Where(i => i.DateTimeIssued >= options.StartDate && i.DateTimeIssued <= options.EndDate)
                .ToList();

            var report = GenerateTaxSummary(filteredInvoices, options);
            return Encoding.UTF8.GetBytes(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax summary report");
            throw;
        }
    }

    public async Task<byte[]> GenerateSalesAnalysisReportAsync(
        SalesAnalysisOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating sales analysis report from {StartDate} to {EndDate}",
                options.StartDate,
                options.EndDate);

            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var receipts = await _unitOfWork.Receipts.GetAllAsync();

            var report = GenerateSalesAnalysis(
                invoices.Where(i => i.DateTimeIssued >= options.StartDate && i.DateTimeIssued <= options.EndDate).ToList(),
                receipts.Where(r => r.DateTimeIssued >= options.StartDate && r.DateTimeIssued <= options.EndDate).ToList(),
                options);

            return Encoding.UTF8.GetBytes(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales analysis report");
            throw;
        }
    }

    // Helper methods for content generation
    private string GenerateInvoicePdfContent(Invoice invoice)
    {
        return $@"INVOICE PDF CONTENT
Invoice Number: {invoice.InvoiceNumber}
Date: {invoice.DateTimeIssued:yyyy-MM-dd}
Customer: {invoice.Customer?.NameEn}
Total Amount: {invoice.TotalAmount:N2} EGP
Status: {invoice.Status}
ETA Long ID: {invoice.EtaLongId}";
    }

    private string GenerateReceiptPdfContent(Receipt receipt)
    {
        return $@"RECEIPT PDF CONTENT
Receipt Number: {receipt.ReceiptNumber}
Date: {receipt.DateTimeIssued:yyyy-MM-dd}
Total Amount: {receipt.TotalAmount:N2} EGP
Payment Method: {receipt.PaymentMethod}";
    }

    private string GenerateInvoicesCsv(List<Invoice> invoices, InvoiceExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Invoice Number,Date,Customer,Net Amount,Tax Amount,Total Amount,Status,ETA Long ID");

        foreach (var invoice in invoices)
        {
            sb.AppendLine($"{invoice.InvoiceNumber}," +
                         $"{invoice.DateTimeIssued:yyyy-MM-dd}," +
                         $"{invoice.Customer?.NameEn}," +
                         $"{invoice.NetAmount:N2}," +
                         $"{invoice.TotalTaxAmount:N2}," +
                         $"{invoice.TotalAmount:N2}," +
                         $"{invoice.Status}," +
                         $"{invoice.EtaLongId}");
        }

        return sb.ToString();
    }

    private string GenerateReceiptsCsv(List<Receipt> receipts, ReceiptExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Receipt Number,Date,Net Amount,Tax Amount,Total Amount,Payment Method");

        foreach (var receipt in receipts)
        {
            sb.AppendLine($"{receipt.ReceiptNumber}," +
                         $"{receipt.DateTimeIssued:yyyy-MM-dd}," +
                         $"{receipt.NetAmount:N2}," +
                         $"{receipt.TotalTaxAmount:N2}," +
                         $"{receipt.TotalAmount:N2}," +
                         $"{receipt.PaymentMethod}");
        }

        return sb.ToString();
    }

    private string GenerateTaxSummary(List<Invoice> invoices, TaxSummaryOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TAX SUMMARY REPORT");
        sb.AppendLine($"Period: {options.StartDate:yyyy-MM-dd} to {options.EndDate:yyyy-MM-dd}");
        sb.AppendLine();

        var totalTax = invoices.Sum(i => i.TotalTaxAmount);
        var totalNet = invoices.Sum(i => i.NetAmount);
        var totalGross = invoices.Sum(i => i.TotalAmount);

        sb.AppendLine($"Total Invoices: {invoices.Count}");
        sb.AppendLine($"Total Net Amount: {totalNet:N2} EGP");
        sb.AppendLine($"Total Tax Amount: {totalTax:N2} EGP");
        sb.AppendLine($"Total Gross Amount: {totalGross:N2} EGP");

        return sb.ToString();
    }

    private string GenerateSalesAnalysis(List<Invoice> invoices, List<Receipt> receipts, SalesAnalysisOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SALES ANALYSIS REPORT");
        sb.AppendLine($"Period: {options.StartDate:yyyy-MM-dd} to {options.EndDate:yyyy-MM-dd}");
        sb.AppendLine();

        var totalInvoices = invoices.Sum(i => i.TotalAmount);
        var totalReceipts = receipts.Sum(r => r.TotalAmount);

        sb.AppendLine($"Invoice Sales (B2B): {totalInvoices:N2} EGP ({invoices.Count} invoices)");
        sb.AppendLine($"Receipt Sales (B2C): {totalReceipts:N2} EGP ({receipts.Count} receipts)");
        sb.AppendLine($"Total Sales: {(totalInvoices + totalReceipts):N2} EGP");

        return sb.ToString();
    }
}
