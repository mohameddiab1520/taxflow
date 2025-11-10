using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Application.Services;

/// <summary>
/// Tax calculation service for Egyptian tax standards
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    // Egyptian VAT rates
    private const decimal StandardVatRate = 14m; // 14% standard rate
    private const decimal TableTaxRate = 14m;
    private const decimal EntertainmentTaxRate = 10m;

    /// <summary>
    /// Calculates taxes for an invoice line
    /// </summary>
    public void CalculateLineTaxes(InvoiceLine line, bool isTaxExempt = false)
    {
        if (line == null)
            throw new ArgumentNullException(nameof(line));

        // Calculate net amount first
        var grossAmount = line.Quantity * line.UnitPrice;
        line.NetAmount = Math.Round(grossAmount - line.Discount, 5);

        // If tax exempt, clear all taxes
        if (isTaxExempt)
        {
            line.TaxItems.Clear();
            line.TotalTaxAmount = 0;
            line.TotalAmount = line.NetAmount;
            return;
        }

        // Calculate each tax type
        foreach (var taxItem in line.TaxItems)
        {
            taxItem.Amount = line.NetAmount;
            taxItem.CalculateTaxValue();
        }

        // Sum all taxes
        line.TotalTaxAmount = Math.Round(line.TaxItems.Sum(t => t.TaxValue), 5);

        // Calculate total with tax
        line.TotalAmount = Math.Round(line.NetAmount + line.TotalTaxAmount, 5);
    }

    /// <summary>
    /// Calculates taxes for a receipt line
    /// </summary>
    public void CalculateLineTaxes(ReceiptLine line, bool isTaxExempt = false)
    {
        if (line == null)
            throw new ArgumentNullException(nameof(line));

        var grossAmount = line.Quantity * line.UnitPrice;
        line.NetAmount = Math.Round(grossAmount - line.Discount, 5);

        if (isTaxExempt)
        {
            line.TaxItems.Clear();
            line.TotalTaxAmount = 0;
            line.TotalAmount = line.NetAmount;
            return;
        }

        foreach (var taxItem in line.TaxItems)
        {
            taxItem.Amount = line.NetAmount;
            taxItem.CalculateTaxValue();
        }

        line.TotalTaxAmount = Math.Round(line.TaxItems.Sum(t => t.TaxValue), 5);
        line.TotalAmount = Math.Round(line.NetAmount + line.TotalTaxAmount, 5);
    }

    /// <summary>
    /// Calculates all totals for an invoice
    /// </summary>
    public void CalculateInvoiceTotals(Invoice invoice)
    {
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));

        // Calculate all line totals first
        foreach (var line in invoice.Lines)
        {
            CalculateLineTaxes(line, invoice.Customer?.IsTaxExempt ?? false);
        }

        // Calculate invoice totals
        invoice.TotalSalesAmount = Math.Round(
            invoice.Lines.Sum(l => l.Quantity * l.UnitPrice), 5);

        invoice.TotalDiscountAmount = Math.Round(
            invoice.Lines.Sum(l => l.Discount) + invoice.ExtraDiscountAmount, 5);

        invoice.NetAmount = Math.Round(
            invoice.Lines.Sum(l => l.NetAmount) - invoice.ExtraDiscountAmount, 5);

        invoice.TotalTaxAmount = Math.Round(
            invoice.Lines.Sum(l => l.TotalTaxAmount), 5);

        invoice.TotalAmount = Math.Round(
            invoice.NetAmount + invoice.TotalTaxAmount, 5);

        // Aggregate tax totals by type
        invoice.TaxTotals = invoice.Lines
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

    /// <summary>
    /// Calculates all totals for a receipt
    /// </summary>
    public void CalculateReceiptTotals(Receipt receipt)
    {
        if (receipt == null)
            throw new ArgumentNullException(nameof(receipt));

        foreach (var line in receipt.Lines)
        {
            CalculateLineTaxes(line, receipt.Customer?.IsTaxExempt ?? false);
        }

        receipt.TotalSalesAmount = Math.Round(
            receipt.Lines.Sum(l => l.Quantity * l.UnitPrice), 5);

        receipt.TotalDiscountAmount = Math.Round(
            receipt.Lines.Sum(l => l.Discount), 5);

        receipt.NetAmount = Math.Round(
            receipt.Lines.Sum(l => l.NetAmount), 5);

        receipt.TotalTaxAmount = Math.Round(
            receipt.Lines.Sum(l => l.TotalTaxAmount), 5);

        receipt.TotalAmount = Math.Round(
            receipt.NetAmount + receipt.TotalTaxAmount, 5);

        receipt.TaxTotals = receipt.Lines
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

        // Calculate change for cash payments
        if (receipt.PaymentMethod == "Cash" && receipt.AmountTendered.HasValue)
        {
            receipt.ChangeReturned = Math.Round(
                receipt.AmountTendered.Value - receipt.TotalAmount, 2);
        }
    }

    /// <summary>
    /// Gets standard VAT tax item
    /// </summary>
    public TaxItem GetStandardVatTaxItem()
    {
        return new TaxItem
        {
            TaxType = TaxType.VAT,
            Rate = StandardVatRate,
            SubType = "T1"
        };
    }

    /// <summary>
    /// Gets table tax item
    /// </summary>
    public TaxItem GetTableTaxItem()
    {
        return new TaxItem
        {
            TaxType = TaxType.TableTax,
            Rate = TableTaxRate,
            SubType = "T2"
        };
    }

    /// <summary>
    /// Gets entertainment tax item
    /// </summary>
    public TaxItem GetEntertainmentTaxItem()
    {
        return new TaxItem
        {
            TaxType = TaxType.EntertainmentTax,
            Rate = EntertainmentTaxRate,
            SubType = "T3"
        };
    }

    /// <summary>
    /// Creates tax items based on item category
    /// </summary>
    public List<TaxItem> GetApplicableTaxes(string itemCategory)
    {
        var taxes = new List<TaxItem>();

        // Default: Standard VAT
        taxes.Add(GetStandardVatTaxItem());

        // Add additional taxes based on category
        switch (itemCategory?.ToUpper())
        {
            case "RESTAURANT":
            case "HOTEL":
                taxes.Add(GetTableTaxItem());
                break;

            case "ENTERTAINMENT":
            case "CINEMA":
            case "THEATER":
                taxes.Add(GetEntertainmentTaxItem());
                break;
        }

        return taxes;
    }

    /// <summary>
    /// Validates tax calculations
    /// </summary>
    public bool ValidateTaxCalculations(Invoice invoice, out List<string> errors)
    {
        errors = new List<string>();

        if (invoice == null)
        {
            errors.Add("Invoice is null");
            return false;
        }

        // Check total amounts match
        var calculatedTotal = invoice.Lines.Sum(l => l.TotalAmount) - invoice.ExtraDiscountAmount;
        var difference = Math.Abs(calculatedTotal - invoice.TotalAmount);

        if (difference > 0.01m) // Allow 1 cent rounding difference
        {
            errors.Add($"Total amount mismatch: Calculated {calculatedTotal:F2} vs Invoice {invoice.TotalAmount:F2}");
        }

        // Validate each line
        foreach (var line in invoice.Lines)
        {
            var lineTotal = line.NetAmount + line.TotalTaxAmount;
            var lineDiff = Math.Abs(lineTotal - line.TotalAmount);

            if (lineDiff > 0.01m)
            {
                errors.Add($"Line {line.LineNumber}: Amount mismatch");
            }

            // Check tax rates are valid
            foreach (var tax in line.TaxItems)
            {
                if (tax.Rate < 0 || tax.Rate > 100)
                {
                    errors.Add($"Line {line.LineNumber}: Invalid tax rate {tax.Rate}%");
                }
            }
        }

        return errors.Count == 0;
    }
}

/// <summary>
/// Tax calculation service interface
/// </summary>
public interface ITaxCalculationService
{
    void CalculateLineTaxes(InvoiceLine line, bool isTaxExempt = false);
    void CalculateLineTaxes(ReceiptLine line, bool isTaxExempt = false);
    void CalculateInvoiceTotals(Invoice invoice);
    void CalculateReceiptTotals(Receipt receipt);
    TaxItem GetStandardVatTaxItem();
    TaxItem GetTableTaxItem();
    TaxItem GetEntertainmentTaxItem();
    List<TaxItem> GetApplicableTaxes(string itemCategory);
    bool ValidateTaxCalculations(Invoice invoice, out List<string> errors);
}
