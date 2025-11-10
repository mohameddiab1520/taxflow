using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using TaxFlow.Application.Services;
using TaxFlow.Application.Validators;
using TaxFlow.Infrastructure.Services.ETA;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Desktop.ViewModels.Invoices;

/// <summary>
/// View model for creating/editing invoices
/// </summary>
public partial class InvoiceViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly IEtaSubmissionService _etaSubmissionService;
    private readonly ILogger<InvoiceViewModel> _logger;

    [ObservableProperty]
    private Guid? _invoiceId;

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _dateTimeIssued = DateTime.Now;

    [ObservableProperty]
    private DocumentType _documentType = DocumentType.Invoice;

    [ObservableProperty]
    private DocumentStatus _status = DocumentStatus.Draft;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private ObservableCollection<InvoiceLineViewModel> _lines = new();

    [ObservableProperty]
    private decimal _extraDiscountAmount = 0;

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
    private string _purchaseOrderReference = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private bool _isNewInvoice = true;

    public InvoiceViewModel(
        IUnitOfWork unitOfWork,
        ITaxCalculationService taxCalculationService,
        IEtaSubmissionService etaSubmissionService,
        ILogger<InvoiceViewModel> logger)
    {
        _unitOfWork = unitOfWork;
        _taxCalculationService = taxCalculationService;
        _etaSubmissionService = etaSubmissionService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync(Guid? invoiceId = null)
    {
        await ExecuteAsync(async () =>
        {
            // Load customers
            var customers = await _unitOfWork.Customers.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customers);

            if (invoiceId.HasValue)
            {
                // Load existing invoice
                await LoadInvoiceAsync(invoiceId.Value);
                IsNewInvoice = false;
            }
            else
            {
                // Generate invoice number
                InvoiceNumber = await GenerateInvoiceNumberAsync();
                IsNewInvoice = true;

                // Add first line
                AddNewLine();
            }
        }, "Loading invoice...");
    }

    /// <summary>
    /// Loads an existing invoice
    /// </summary>
    private async Task LoadInvoiceAsync(Guid id)
    {
        var invoice = await _unitOfWork.Invoices.GetWithDetailsAsync(id);
        if (invoice == null)
        {
            SetError("Invoice not found");
            return;
        }

        InvoiceId = invoice.Id;
        InvoiceNumber = invoice.InvoiceNumber;
        DateTimeIssued = invoice.DateTimeIssued;
        DocumentType = invoice.DocumentType;
        Status = invoice.Status;
        SelectedCustomer = invoice.Customer;
        ExtraDiscountAmount = invoice.ExtraDiscountAmount;
        PurchaseOrderReference = invoice.PurchaseOrderReference ?? string.Empty;
        Notes = invoice.Notes ?? string.Empty;

        // Load lines
        Lines.Clear();
        foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
        {
            var lineVm = new InvoiceLineViewModel(_taxCalculationService);
            lineVm.LoadFromEntity(line);
            Lines.Add(lineVm);
        }

        RecalculateTotals();
    }

    /// <summary>
    /// Adds a new line to the invoice
    /// </summary>
    [RelayCommand]
    private void AddNewLine()
    {
        var lineVm = new InvoiceLineViewModel(_taxCalculationService)
        {
            LineNumber = Lines.Count + 1,
            UnitType = "EA",
            Quantity = 1,
            HasVat = true,
            VatRate = 14m
        };

        Lines.Add(lineVm);
    }

    /// <summary>
    /// Removes a line from the invoice
    /// </summary>
    [RelayCommand]
    private void RemoveLine(InvoiceLineViewModel line)
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
    /// Recalculates all invoice totals
    /// </summary>
    [RelayCommand]
    private void RecalculateTotals()
    {
        // Create temporary invoice for calculation
        var invoice = ToEntity();

        // Calculate totals
        _taxCalculationService.CalculateInvoiceTotals(invoice);

        // Update view model
        TotalSalesAmount = invoice.TotalSalesAmount;
        TotalDiscountAmount = invoice.TotalDiscountAmount;
        NetAmount = invoice.NetAmount;
        TotalTaxAmount = invoice.TotalTaxAmount;
        TotalAmount = invoice.TotalAmount;
    }

    /// <summary>
    /// Validates the invoice
    /// </summary>
    [RelayCommand]
    private async Task ValidateAsync()
    {
        await ExecuteAsync(async () =>
        {
            var invoice = ToEntity();
            var validator = new InvoiceValidator();
            var result = await validator.ValidateAsync(invoice);

            if (result.IsValid)
            {
                // Validate tax calculations
                if (_taxCalculationService.ValidateTaxCalculations(invoice, out var taxErrors))
                {
                    IsValid = true;
                    ValidationMessage = "✓ Invoice is valid and ready for submission";
                    Status = DocumentStatus.Valid;
                }
                else
                {
                    IsValid = false;
                    ValidationMessage = "✗ Tax calculation errors:\n" + string.Join("\n", taxErrors);
                    Status = DocumentStatus.Invalid;
                }
            }
            else
            {
                IsValid = false;
                var errors = result.Errors.Select(e => $"• {e.ErrorMessage}");
                ValidationMessage = "✗ Validation errors:\n" + string.Join("\n", errors);
                Status = DocumentStatus.Invalid;
            }
        }, "Validating invoice...");
    }

    /// <summary>
    /// Saves the invoice
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteAsync(async () =>
        {
            var invoice = ToEntity();

            if (IsNewInvoice)
            {
                await _unitOfWork.Invoices.AddAsync(invoice);
                InvoiceId = invoice.Id;
            }
            else
            {
                await _unitOfWork.Invoices.UpdateAsync(invoice);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} saved successfully", InvoiceNumber);
            IsNewInvoice = false;

        }, "Saving invoice...");
    }

    /// <summary>
    /// Submits the invoice to ETA
    /// </summary>
    [RelayCommand]
    private async Task SubmitToEtaAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Validate first
            await ValidateAsync();

            if (!IsValid)
            {
                SetError("Cannot submit invalid invoice. Please fix validation errors first.");
                return;
            }

            // Save if not saved
            if (IsNewInvoice || InvoiceId == null)
            {
                await SaveAsync();
            }

            var invoice = await _unitOfWork.Invoices.GetWithDetailsAsync(InvoiceId!.Value);
            if (invoice == null)
            {
                SetError("Invoice not found");
                return;
            }

            // Update status
            invoice.Status = DocumentStatus.Submitting;
            await _unitOfWork.Invoices.UpdateAsync(invoice);
            await _unitOfWork.CommitAsync();
            Status = DocumentStatus.Submitting;

            // Submit to ETA
            var result = await _etaSubmissionService.SubmitInvoiceAsync(invoice);

            if (result.IsSuccess)
            {
                invoice.Status = DocumentStatus.Submitted;
                invoice.EtaSubmissionId = result.SubmissionId;
                invoice.EtaLongId = result.LongId;
                invoice.EtaInternalId = result.InternalId;
                invoice.EtaResponseDate = result.SubmittedAt;

                await _unitOfWork.Invoices.UpdateAsync(invoice);
                await _unitOfWork.CommitAsync();

                Status = DocumentStatus.Submitted;
                ValidationMessage = $"✓ Successfully submitted to ETA\nLong ID: {result.LongId}";

                _logger.LogInformation("Invoice {InvoiceNumber} submitted successfully. Long ID: {LongId}",
                    InvoiceNumber, result.LongId);
            }
            else
            {
                invoice.Status = DocumentStatus.Failed;
                invoice.ValidationErrors = result.ErrorMessage;

                await _unitOfWork.Invoices.UpdateAsync(invoice);
                await _unitOfWork.CommitAsync();

                Status = DocumentStatus.Failed;
                SetError($"ETA submission failed: {result.ErrorMessage}");

                _logger.LogError("Invoice {InvoiceNumber} submission failed: {Error}",
                    InvoiceNumber, result.ErrorMessage);
            }
        }, "Submitting to ETA...");
    }

    /// <summary>
    /// Converts view model to entity
    /// </summary>
    private Invoice ToEntity()
    {
        var invoice = new Invoice
        {
            Id = InvoiceId ?? Guid.NewGuid(),
            InvoiceNumber = InvoiceNumber,
            DateTimeIssued = DateTimeIssued,
            DocumentType = DocumentType,
            DocumentTypeVersion = "1.0",
            Status = Status,
            CustomerId = SelectedCustomer?.Id ?? Guid.Empty,
            Customer = SelectedCustomer,
            ExtraDiscountAmount = ExtraDiscountAmount,
            PurchaseOrderReference = PurchaseOrderReference,
            Notes = Notes,
            TotalSalesAmount = TotalSalesAmount,
            TotalDiscountAmount = TotalDiscountAmount,
            NetAmount = NetAmount,
            TotalTaxAmount = TotalTaxAmount,
            TotalAmount = TotalAmount
        };

        // Add lines
        foreach (var lineVm in Lines)
        {
            var line = lineVm.ToEntity();
            line.InvoiceId = invoice.Id;
            invoice.Lines.Add(line);
        }

        return invoice;
    }

    /// <summary>
    /// Generates a new invoice number
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var lastInvoice = (await _unitOfWork.Invoices.GetAllAsync())
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault();

        if (lastInvoice != null && int.TryParse(lastInvoice.InvoiceNumber, out var lastNumber))
        {
            return (lastNumber + 1).ToString("D8");
        }

        return DateTime.Now.ToString("yyyyMMdd") + "0001";
    }

    /// <summary>
    /// Property changed handlers
    /// </summary>
    partial void OnExtraDiscountAmountChanged(decimal value)
    {
        RecalculateTotals();
    }
}
