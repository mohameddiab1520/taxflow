using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using TaxFlow.Core.ValueObjects;
using TaxFlow.Application.Services;
using TaxFlow.Application.Validators;
using TaxFlow.Infrastructure.Services.ETA;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Desktop.ViewModels.Receipts;

/// <summary>
/// View model for POS receipt entry (B2C transactions)
/// </summary>
public partial class ReceiptViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly IEtaSubmissionService _etaSubmissionService;
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReceiptViewModel> _logger;

    [ObservableProperty]
    private Guid? _receiptId;

    [ObservableProperty]
    private string _receiptNumber = string.Empty;

    [ObservableProperty]
    private DateTime _dateTimeIssued = DateTime.Now;

    [ObservableProperty]
    private DocumentStatus _status = DocumentStatus.Draft;

    [ObservableProperty]
    private string _terminalId = "POS-01";

    [ObservableProperty]
    private string _cashierId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ReceiptLineItem> _lines = new();

    [ObservableProperty]
    private string _paymentMethod = "Cash";

    [ObservableProperty]
    private decimal? _amountTendered;

    [ObservableProperty]
    private decimal _changeReturned = 0;

    [ObservableProperty]
    private decimal _totalSalesAmount = 0;

    [ObservableProperty]
    private decimal _totalDiscountAmount = 0;

    [ObservableProperty]
    private decimal _netAmount = 0;

    [ObservableProperty]
    private decimal _totalTaxAmount = 0;

    [ObservableProperty]
    private decimal _totalAmount = 0;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isValid = false;

    // Quick item entry
    [ObservableProperty]
    private string _quickItemName = string.Empty;

    [ObservableProperty]
    private decimal _quickQuantity = 1;

    [ObservableProperty]
    private decimal _quickPrice = 0;

    public List<string> PaymentMethods { get; } = new() { "Cash", "Card", "Mobile", "Bank Transfer" };
    public List<string> Terminals { get; } = new() { "POS-01", "POS-02", "POS-03", "POS-04", "POS-05" };

    public ReceiptViewModel(
        IUnitOfWork unitOfWork,
        ITaxCalculationService taxCalculationService,
        IEtaSubmissionService etaSubmissionService,
        IReportingService reportingService,
        ILogger<ReceiptViewModel> logger)
    {
        _unitOfWork = unitOfWork;
        _taxCalculationService = taxCalculationService;
        _etaSubmissionService = etaSubmissionService;
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            ReceiptNumber = await GenerateReceiptNumberAsync();
            CashierId = Environment.UserName;

        }, "Initializing POS...");
    }

    /// <summary>
    /// Quick add item - optimized for fast POS entry
    /// </summary>
    [RelayCommand]
    private void QuickAddItem()
    {
        if (string.IsNullOrWhiteSpace(QuickItemName) || QuickPrice <= 0)
        {
            SetError("Please enter item name and price");
            return;
        }

        var lineItem = new ReceiptLineItem
        {
            LineNumber = Lines.Count + 1,
            DescriptionEn = QuickItemName,
            DescriptionAr = TranslateToArabic(QuickItemName),
            Quantity = QuickQuantity,
            UnitPrice = QuickPrice,
            UnitType = "EA",
            HasVat = true,
            VatRate = 14m
        };

        lineItem.RecalculateAmounts(_taxCalculationService);
        Lines.Add(lineItem);

        // Clear quick entry fields
        QuickItemName = string.Empty;
        QuickQuantity = 1;
        QuickPrice = 0;

        RecalculateTotals();
    }

    /// <summary>
    /// Removes a line from the receipt
    /// </summary>
    [RelayCommand]
    private void RemoveLine(ReceiptLineItem line)
    {
        Lines.Remove(line);

        // Renumber lines
        for (int i = 0; i < Lines.Count; i++)
        {
            Lines[i].LineNumber = i + 1;
        }

        RecalculateTotals();
    }

    /// <summary>
    /// Recalculates all receipt totals
    /// </summary>
    [RelayCommand]
    private void RecalculateTotals()
    {
        var receipt = ToEntity();
        _taxCalculationService.CalculateReceiptTotals(receipt);

        TotalSalesAmount = receipt.TotalSalesAmount;
        TotalDiscountAmount = receipt.TotalDiscountAmount;
        NetAmount = receipt.NetAmount;
        TotalTaxAmount = receipt.TotalTaxAmount;
        TotalAmount = receipt.TotalAmount;

        // Calculate change for cash
        if (PaymentMethod == "Cash" && AmountTendered.HasValue)
        {
            ChangeReturned = Math.Round(AmountTendered.Value - TotalAmount, 2);
        }
        else
        {
            ChangeReturned = 0;
        }
    }

    /// <summary>
    /// Validates the receipt
    /// </summary>
    [RelayCommand]
    private async Task ValidateAsync()
    {
        await ExecuteAsync(async () =>
        {
            var receipt = ToEntity();
            var validator = new ReceiptValidator();
            var result = await validator.ValidateAsync(receipt);

            if (result.IsValid)
            {
                IsValid = true;
                ValidationMessage = "✓ Receipt is valid";
                Status = DocumentStatus.Valid;
            }
            else
            {
                IsValid = false;
                var errors = result.Errors.Select(e => $"• {e.ErrorMessage}");
                ValidationMessage = "✗ Validation errors:\n" + string.Join("\n", errors);
                Status = DocumentStatus.Invalid;
            }
        }, "Validating receipt...");
    }

    /// <summary>
    /// Completes the sale and saves receipt
    /// </summary>
    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Validate first
            await ValidateAsync();

            if (!IsValid)
            {
                SetError("Cannot complete sale. Please fix validation errors.");
                return;
            }

            var receipt = ToEntity();
            await _unitOfWork.Receipts.AddAsync(receipt);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Receipt {ReceiptNumber} completed. Total: {Total:C}",
                ReceiptNumber, TotalAmount);

            // Print receipt
            await PrintReceiptAsync(receipt.Id);

            // Submit to ETA (optional for B2C)
            var shouldSubmit = MessageBox.Show(
                "Do you want to submit this receipt to ETA?",
                "Submit to ETA",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (shouldSubmit)
            {
                ReceiptId = receipt.Id;
                await SubmitToEtaAsync();
            }

            // Reset for next sale
            await ResetReceiptAsync();

        }, "Completing sale...");
    }

    /// <summary>
    /// Submits the receipt to ETA
    /// </summary>
    [RelayCommand]
    private async Task SubmitToEtaAsync()
    {
        await ExecuteAsync(async () =>
        {
            var receipt = await _unitOfWork.Receipts.GetByIdAsync(ReceiptId!.Value);
            if (receipt == null)
            {
                SetError("Receipt not found");
                return;
            }

            receipt.Status = DocumentStatus.Submitting;
            await _unitOfWork.Receipts.UpdateAsync(receipt);
            await _unitOfWork.CommitAsync();

            var result = await _etaSubmissionService.SubmitReceiptAsync(receipt);

            if (result.IsSuccess)
            {
                receipt.Status = DocumentStatus.Submitted;
                receipt.EtaSubmissionReference = result.LongId;
                receipt.EtaResponseDate = result.SubmittedAt;

                ValidationMessage = "✓ Successfully submitted to ETA";
                _logger.LogInformation("Receipt {ReceiptNumber} submitted to ETA", ReceiptNumber);
            }
            else
            {
                receipt.Status = DocumentStatus.Failed;
                receipt.ValidationErrors = result.ErrorMessage;

                SetError($"ETA submission failed: {result.ErrorMessage}");
                _logger.LogError("Receipt {ReceiptNumber} submission failed", ReceiptNumber);
            }

            await _unitOfWork.Receipts.UpdateAsync(receipt);
            await _unitOfWork.CommitAsync();

        }, "Submitting to ETA...");
    }

    /// <summary>
    /// Resets the receipt for a new sale
    /// </summary>
    [RelayCommand]
    private async Task ResetReceiptAsync()
    {
        ReceiptNumber = await GenerateReceiptNumberAsync();
        Lines.Clear();
        PaymentMethod = "Cash";
        AmountTendered = null;
        ChangeReturned = 0;
        Notes = string.Empty;
        ValidationMessage = string.Empty;
        IsValid = false;
        Status = DocumentStatus.Draft;

        TotalSalesAmount = 0;
        TotalDiscountAmount = 0;
        NetAmount = 0;
        TotalTaxAmount = 0;
        TotalAmount = 0;

        QuickItemName = string.Empty;
        QuickQuantity = 1;
        QuickPrice = 0;
    }

    /// <summary>
    /// Converts view model to entity
    /// </summary>
    private Receipt ToEntity()
    {
        var receipt = new Receipt
        {
            Id = ReceiptId ?? Guid.NewGuid(),
            ReceiptNumber = ReceiptNumber,
            DateTimeIssued = DateTimeIssued,
            Status = Status,
            TerminalId = TerminalId,
            CashierId = CashierId,
            PaymentMethod = PaymentMethod,
            AmountTendered = AmountTendered,
            ChangeReturned = ChangeReturned,
            Notes = Notes,
            TotalSalesAmount = TotalSalesAmount,
            TotalDiscountAmount = TotalDiscountAmount,
            NetAmount = NetAmount,
            TotalTaxAmount = TotalTaxAmount,
            TotalAmount = TotalAmount
        };

        foreach (var lineItem in Lines)
        {
            var line = new ReceiptLine
            {
                LineNumber = lineItem.LineNumber,
                DescriptionEn = lineItem.DescriptionEn,
                DescriptionAr = lineItem.DescriptionAr,
                ItemCode = lineItem.ItemCode,
                UnitType = lineItem.UnitType,
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice,
                Discount = lineItem.Discount,
                NetAmount = lineItem.NetAmount,
                TotalTaxAmount = lineItem.TotalTaxAmount,
                TotalAmount = lineItem.TotalAmount
            };

            if (lineItem.HasVat)
            {
                line.TaxItems.Add(new TaxItem
                {
                    TaxType = TaxType.VAT,
                    Rate = lineItem.VatRate,
                    SubType = "T1",
                    Amount = lineItem.NetAmount,
                    TaxValue = lineItem.TotalTaxAmount
                });
            }

            receipt.Lines.Add(line);
        }

        return receipt;
    }

    /// <summary>
    /// Generates a new receipt number
    /// </summary>
    private async Task<string> GenerateReceiptNumberAsync()
    {
        var lastReceipt = (await _unitOfWork.Receipts.GetAllAsync())
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (lastReceipt != null && int.TryParse(lastReceipt.ReceiptNumber.Replace("RCP-", ""), out var lastNumber))
        {
            return $"RCP-{(lastNumber + 1):D8}";
        }

        return "RCP-" + DateTime.Now.ToString("yyyyMMdd") + "0001";
    }

    /// <summary>
    /// Property changed handlers
    /// </summary>
    partial void OnAmountTenderedChanged(decimal? value)
    {
        RecalculateTotals();
    }

    partial void OnPaymentMethodChanged(string value)
    {
        if (value != "Cash")
        {
            AmountTendered = TotalAmount;
        }
    }

    /// <summary>
    /// Prints receipt using reporting service
    /// </summary>
    private async Task PrintReceiptAsync(Guid receiptId)
    {
        try
        {
            var pdfBytes = await _reportingService.GenerateReceiptPdfAsync(receiptId);

            // Save to temp file and open with default PDF viewer
            var tempFile = Path.Combine(Path.GetTempPath(), $"Receipt_{ReceiptNumber}.pdf");
            await File.WriteAllBytesAsync(tempFile, pdfBytes);

            // Open PDF with default application
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processInfo);

            _logger.LogInformation("Receipt {ReceiptNumber} printed successfully", ReceiptNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing receipt {ReceiptNumber}", ReceiptNumber);
            MessageBox.Show(
                $"Failed to print receipt: {ex.Message}",
                "Print Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Translates text to Arabic (simple dictionary-based approach)
    /// </summary>
    private string TranslateToArabic(string englishText)
    {
        // Simple translation dictionary for common POS items
        var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Water", "ماء" },
            { "Juice", "عصير" },
            { "Coffee", "قهوة" },
            { "Tea", "شاي" },
            { "Bread", "خبز" },
            { "Milk", "لبن" },
            { "Rice", "أرز" },
            { "Sugar", "سكر" },
            { "Salt", "ملح" },
            { "Oil", "زيت" },
            { "Chicken", "دجاج" },
            { "Beef", "لحم بقري" },
            { "Fish", "سمك" },
            { "Vegetables", "خضروات" },
            { "Fruits", "فواكه" },
            { "Cheese", "جبنة" },
            { "Eggs", "بيض" },
            { "Butter", "زبدة" },
            { "Yogurt", "زبادي" },
            { "Pasta", "مكرونة" }
        };

        return translations.TryGetValue(englishText, out var arabicText)
            ? arabicText
            : englishText; // Return original if no translation found
    }
}

/// <summary>
/// Receipt line item for quick POS entry
/// </summary>
public partial class ReceiptLineItem : ObservableObject
{
    [ObservableProperty]
    private int _lineNumber;

    [ObservableProperty]
    private string _descriptionEn = string.Empty;

    [ObservableProperty]
    private string _descriptionAr = string.Empty;

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

    public decimal GrossAmount => Quantity * UnitPrice;

    public void RecalculateAmounts(ITaxCalculationService taxCalculationService)
    {
        var line = new ReceiptLine
        {
            Quantity = Quantity,
            UnitPrice = UnitPrice,
            Discount = Discount
        };

        if (HasVat)
        {
            line.TaxItems.Add(new TaxItem
            {
                TaxType = TaxType.VAT,
                Rate = VatRate,
                SubType = "T1"
            });
        }

        taxCalculationService.CalculateLineTaxes(line, !HasVat);

        NetAmount = line.NetAmount;
        TotalTaxAmount = line.TotalTaxAmount;
        TotalAmount = line.TotalAmount;
    }
}
