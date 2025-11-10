using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using TaxFlow.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Desktop.ViewModels.Invoices;

/// <summary>
/// View model for invoice list with search and filtering
/// </summary>
public partial class InvoiceListViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly ILogger<InvoiceListViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<Invoice> _invoices = new();

    [ObservableProperty]
    private Invoice? _selectedInvoice;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DocumentStatus? _filterStatus;

    [ObservableProperty]
    private DateTime? _filterStartDate;

    [ObservableProperty]
    private DateTime? _filterEndDate;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _draftCount;

    [ObservableProperty]
    private int _submittedCount;

    [ObservableProperty]
    private int _acceptedCount;

    [ObservableProperty]
    private int _rejectedCount;

    public InvoiceListViewModel(
        IUnitOfWork unitOfWork,
        INavigationService navigationService,
        ILogger<InvoiceListViewModel> logger)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _logger = logger;

        // Set default date filter (last 30 days)
        FilterEndDate = DateTime.Now;
        FilterStartDate = DateTime.Now.AddDays(-30);
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadInvoicesAsync();
        await LoadStatisticsAsync();
    }

    /// <summary>
    /// Loads invoices based on current filters
    /// </summary>
    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        await ExecuteAsync(async () =>
        {
            IEnumerable<Invoice> invoices;

            // Apply filters
            if (FilterStatus.HasValue)
            {
                invoices = await _unitOfWork.Invoices.GetByStatusAsync(FilterStatus.Value);
            }
            else if (FilterStartDate.HasValue && FilterEndDate.HasValue)
            {
                invoices = await _unitOfWork.Invoices.GetByDateRangeAsync(
                    FilterStartDate.Value,
                    FilterEndDate.Value.AddDays(1));
            }
            else
            {
                invoices = await _unitOfWork.Invoices.GetAllAsync();
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                invoices = invoices.Where(i =>
                    i.InvoiceNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (i.Customer?.NameEn?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Customer?.NameAr?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.PurchaseOrderReference?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Sort by date (newest first)
            invoices = invoices.OrderByDescending(i => i.DateTimeIssued);

            Invoices = new ObservableCollection<Invoice>(invoices);
            TotalCount = Invoices.Count;

            _logger.LogInformation("Loaded {Count} invoices", TotalCount);

        }, "Loading invoices...");
    }

    /// <summary>
    /// Loads invoice statistics
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var startDate = FilterStartDate ?? DateTime.Now.AddDays(-30);
            var endDate = FilterEndDate ?? DateTime.Now;

            var stats = await _unitOfWork.Invoices.GetSubmissionStatsAsync(startDate, endDate);

            DraftCount = stats.GetValueOrDefault(DocumentStatus.Draft, 0);
            SubmittedCount = stats.GetValueOrDefault(DocumentStatus.Submitted, 0);
            AcceptedCount = stats.GetValueOrDefault(DocumentStatus.Accepted, 0);
            RejectedCount = stats.GetValueOrDefault(DocumentStatus.Rejected, 0);

        }, "Loading statistics...");
    }

    /// <summary>
    /// Creates a new invoice
    /// </summary>
    [RelayCommand]
    private void CreateNewInvoice()
    {
        // Navigate to invoice creation view
        _logger.LogInformation("Creating new invoice");
        // TODO: Navigate to InvoiceViewModel
    }

    /// <summary>
    /// Edits the selected invoice
    /// </summary>
    [RelayCommand]
    private void EditInvoice(Invoice invoice)
    {
        if (invoice == null)
            return;

        _logger.LogInformation("Editing invoice {InvoiceNumber}", invoice.InvoiceNumber);
        // TODO: Navigate to InvoiceViewModel with invoice ID
    }

    /// <summary>
    /// Deletes the selected invoice
    /// </summary>
    [RelayCommand]
    private async Task DeleteInvoiceAsync(Invoice invoice)
    {
        if (invoice == null)
            return;

        // TODO: Show confirmation dialog
        await ExecuteAsync(async () =>
        {
            await _unitOfWork.Invoices.DeleteAsync(invoice);
            await _unitOfWork.CommitAsync();

            Invoices.Remove(invoice);
            TotalCount = Invoices.Count;

            _logger.LogInformation("Deleted invoice {InvoiceNumber}", invoice.InvoiceNumber);

        }, "Deleting invoice...");
    }

    /// <summary>
    /// Exports invoices to Excel
    /// </summary>
    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        await ExecuteAsync(async () =>
        {
            // TODO: Implement Excel export
            _logger.LogInformation("Exporting {Count} invoices to Excel", Invoices.Count);

        }, "Exporting to Excel...");
    }

    /// <summary>
    /// Clears all filters
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        FilterStatus = null;
        FilterStartDate = DateTime.Now.AddDays(-30);
        FilterEndDate = DateTime.Now;

        await LoadInvoicesAsync();
    }

    /// <summary>
    /// Refreshes the invoice list
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadInvoicesAsync();
        await LoadStatisticsAsync();
    }

    /// <summary>
    /// Property changed handlers for auto-search
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        // Debounce search
        _ = LoadInvoicesAsync();
    }

    partial void OnFilterStatusChanged(DocumentStatus? value)
    {
        _ = LoadInvoicesAsync();
    }

    partial void OnFilterStartDateChanged(DateTime? value)
    {
        _ = LoadInvoicesAsync();
        _ = LoadStatisticsAsync();
    }

    partial void OnFilterEndDateChanged(DateTime? value)
    {
        _ = LoadInvoicesAsync();
        _ = LoadStatisticsAsync();
    }
}
