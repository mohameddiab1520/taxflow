using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.ValueObjects;
using TaxFlow.Application.Services;

namespace TaxFlow.Desktop.ViewModels.Invoices;

/// <summary>
/// View model for invoice line item
/// </summary>
public partial class InvoiceLineViewModel : ObservableObject
{
    private readonly ITaxCalculationService _taxCalculationService;

    [ObservableProperty]
    private int _lineNumber;

    [ObservableProperty]
    private string _descriptionAr = string.Empty;

    [ObservableProperty]
    private string _descriptionEn = string.Empty;

    [ObservableProperty]
    private string _itemCode = string.Empty;

    [ObservableProperty]
    private string _unitType = "EA";

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice = 0;

    [ObservableProperty]
    private decimal _discount = 0;

    [ObservableProperty]
    private decimal _netAmount = 0;

    [ObservableProperty]
    private decimal _totalTaxAmount = 0;

    [ObservableProperty]
    private decimal _totalAmount = 0;

    [ObservableProperty]
    private bool _hasVat = true;

    [ObservableProperty]
    private decimal _vatRate = 14m;

    public InvoiceLineViewModel(ITaxCalculationService taxCalculationService)
    {
        _taxCalculationService = taxCalculationService;
    }

    /// <summary>
    /// Calculated gross amount (quantity × unit price)
    /// </summary>
    public decimal GrossAmount => Quantity * UnitPrice;

    /// <summary>
    /// Recalculates all amounts when properties change
    /// </summary>
    [RelayCommand]
    private void RecalculateAmounts()
    {
        // Create temporary invoice line for calculation
        var line = ToEntity();

        // Calculate taxes
        _taxCalculationService.CalculateLineTaxes(line, !HasVat);

        // Update view model properties
        NetAmount = line.NetAmount;
        TotalTaxAmount = line.TotalTaxAmount;
        TotalAmount = line.TotalAmount;

        OnPropertyChanged(nameof(GrossAmount));
    }

    /// <summary>
    /// Converts view model to entity
    /// </summary>
    public InvoiceLine ToEntity()
    {
        var line = new InvoiceLine
        {
            LineNumber = LineNumber,
            DescriptionAr = DescriptionAr,
            DescriptionEn = DescriptionEn,
            ItemCode = ItemCode,
            UnitType = UnitType,
            Quantity = Quantity,
            UnitPrice = UnitPrice,
            Discount = Discount,
            NetAmount = NetAmount,
            TotalTaxAmount = TotalTaxAmount,
            TotalAmount = TotalAmount
        };

        // Add VAT if applicable
        if (HasVat)
        {
            line.TaxItems.Add(new TaxItem
            {
                TaxType = TaxType.VAT,
                Rate = VatRate,
                SubType = "T1",
                Amount = 0,
                TaxValue = 0
            });
        }

        return line;
    }

    /// <summary>
    /// Loads entity data into view model
    /// </summary>
    public void LoadFromEntity(InvoiceLine line)
    {
        LineNumber = line.LineNumber;
        DescriptionAr = line.DescriptionAr;
        DescriptionEn = line.DescriptionEn;
        ItemCode = line.ItemCode ?? string.Empty;
        UnitType = line.UnitType;
        Quantity = line.Quantity;
        UnitPrice = line.UnitPrice;
        Discount = line.Discount;
        NetAmount = line.NetAmount;
        TotalTaxAmount = line.TotalTaxAmount;
        TotalAmount = line.TotalAmount;

        // Check if has VAT
        var vatTax = line.TaxItems.FirstOrDefault(t => t.TaxType == TaxType.VAT);
        HasVat = vatTax != null;
        if (vatTax != null)
        {
            VatRate = vatTax.Rate;
        }
    }

    /// <summary>
    /// Property changed handler for auto-calculation
    /// </summary>
    partial void OnQuantityChanged(decimal value)
    {
        RecalculateAmounts();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        RecalculateAmounts();
    }

    partial void OnDiscountChanged(decimal value)
    {
        RecalculateAmounts();
    }

    partial void OnHasVatChanged(bool value)
    {
        RecalculateAmounts();
    }

    partial void OnVatRateChanged(decimal value)
    {
        RecalculateAmounts();
    }
}
