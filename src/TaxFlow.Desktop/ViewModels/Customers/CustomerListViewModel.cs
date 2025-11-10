using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Desktop.Services;
using Microsoft.Extensions.Logging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace TaxFlow.Desktop.ViewModels.Customers;

/// <summary>
/// View model for customer list with search
/// </summary>
public partial class CustomerListViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IReportingService _reportingService;
    private readonly ILogger<CustomerListViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _filterCustomerType;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _businessCount;

    [ObservableProperty]
    private int _personCount;

    [ObservableProperty]
    private int _foreignCount;

    public List<string> CustomerTypes { get; } = new() { "All", "B", "P", "F" };

    public CustomerListViewModel(
        IUnitOfWork unitOfWork,
        INavigationService navigationService,
        IReportingService reportingService,
        ILogger<CustomerListViewModel> logger)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCustomersAsync();
        await LoadStatisticsAsync();
    }

    /// <summary>
    /// Loads customers based on current filters
    /// </summary>
    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();

            // Apply customer type filter
            if (!string.IsNullOrEmpty(FilterCustomerType) && FilterCustomerType != "All")
            {
                customers = customers.Where(c => c.CustomerType == FilterCustomerType);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                customers = customers.Where(c =>
                    c.NameEn.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.NameAr.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (c.TaxRegistrationNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Phone?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Sort by name
            customers = customers.OrderBy(c => c.NameEn);

            Customers = new ObservableCollection<Customer>(customers);
            TotalCount = Customers.Count;

            _logger.LogInformation("Loaded {Count} customers", TotalCount);

        }, "Loading customers...");
    }

    /// <summary>
    /// Loads customer statistics
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var allCustomers = await _unitOfWork.Customers.GetAllAsync();

            BusinessCount = allCustomers.Count(c => c.CustomerType == "B");
            PersonCount = allCustomers.Count(c => c.CustomerType == "P");
            ForeignCount = allCustomers.Count(c => c.CustomerType == "F");

        }, "Loading statistics...");
    }

    /// <summary>
    /// Creates a new customer
    /// </summary>
    [RelayCommand]
    private void CreateNewCustomer()
    {
        _logger.LogInformation("Creating new customer");
        _navigationService.NavigateTo<CustomerViewModel>();
    }

    /// <summary>
    /// Edits the selected customer
    /// </summary>
    [RelayCommand]
    private void EditCustomer(Customer customer)
    {
        if (customer == null)
            return;

        _logger.LogInformation("Editing customer {Name}", customer.NameEn);
        _navigationService.NavigateTo<CustomerViewModel>(customer.Id);
    }

    /// <summary>
    /// Deletes the selected customer
    /// </summary>
    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer customer)
    {
        if (customer == null)
            return;

        // Check if customer has invoices or receipts
        var invoices = await _unitOfWork.Invoices.GetByCustomerAsync(customer.Id);
        if (invoices.Any())
        {
            SetError($"Cannot delete customer '{customer.NameEn}' because they have {invoices.Count()} invoice(s).");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _unitOfWork.Customers.DeleteAsync(customer);
            await _unitOfWork.CommitAsync();

            Customers.Remove(customer);
            TotalCount = Customers.Count;

            _logger.LogInformation("Deleted customer {Name}", customer.NameEn);

        }, "Deleting customer...");
    }

    /// <summary>
    /// Exports customers to Excel
    /// </summary>
    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Exporting {Count} customers to Excel", Customers.Count);

            // Create Excel file manually for customers
            var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Customers");

            // Header
            worksheet.Cell(1, 1).Value = "Name (English)";
            worksheet.Cell(1, 2).Value = "Name (Arabic)";
            worksheet.Cell(1, 3).Value = "Tax Registration";
            worksheet.Cell(1, 4).Value = "Customer Type";
            worksheet.Cell(1, 5).Value = "Email";
            worksheet.Cell(1, 6).Value = "Phone";
            worksheet.Cell(1, 7).Value = "Country";
            worksheet.Cell(1, 8).Value = "City";

            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            // Data
            int row = 2;
            foreach (var customer in Customers)
            {
                worksheet.Cell(row, 1).Value = customer.NameEn;
                worksheet.Cell(row, 2).Value = customer.NameAr;
                worksheet.Cell(row, 3).Value = customer.TaxRegistrationNumber ?? "";
                worksheet.Cell(row, 4).Value = customer.CustomerType;
                worksheet.Cell(row, 5).Value = customer.Email ?? "";
                worksheet.Cell(row, 6).Value = customer.Phone ?? "";
                worksheet.Cell(row, 7).Value = customer.CountryCode ?? "";
                worksheet.Cell(row, 8).Value = customer.CityEn ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var excelBytes = stream.ToArray();

            // Save file dialog
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = $"Customers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Export Customers to Excel"
            };

            if (saveDialog.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(saveDialog.FileName, excelBytes);
                _logger.LogInformation("Customers exported successfully to {Path}", saveDialog.FileName);

                await ShowMessageDialogAsync(
                    "Export Successful",
                    $"Customers have been exported successfully to:\n{saveDialog.FileName}");
            }

        }, "Exporting to Excel...");
    }

    /// <summary>
    /// Clears all filters
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        FilterCustomerType = "All";
        await LoadCustomersAsync();
    }

    /// <summary>
    /// Refreshes the customer list
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadCustomersAsync();
        await LoadStatisticsAsync();
    }

    /// <summary>
    /// Property changed handlers for auto-search
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        _ = LoadCustomersAsync();
    }

    partial void OnFilterCustomerTypeChanged(string? value)
    {
        _ = LoadCustomersAsync();
    }

    /// <summary>
    /// Shows message dialog
    /// </summary>
    private async Task ShowMessageDialogAsync(string title, string message)
    {
        try
        {
            var mainWindow = Application.Current.MainWindow as MetroWindow;
            if (mainWindow == null)
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await mainWindow.ShowMessageAsync(title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing message dialog");
        }
    }
}
