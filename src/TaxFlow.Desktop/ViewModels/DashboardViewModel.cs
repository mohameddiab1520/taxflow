using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Desktop.ViewModels;

/// <summary>
/// Dashboard view model with statistics and charts
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty]
    private int _totalInvoices;

    [ObservableProperty]
    private int _totalReceipts;

    [ObservableProperty]
    private int _pendingSubmission;

    [ObservableProperty]
    private int _submittedToday;

    [ObservableProperty]
    private int _acceptedCount;

    [ObservableProperty]
    private int _rejectedCount;

    [ObservableProperty]
    private decimal _totalRevenueToday;

    [ObservableProperty]
    private decimal _totalRevenueMonth;

    [ObservableProperty]
    private decimal _totalTaxCollected;

    [ObservableProperty]
    private ObservableCollection<DailyStats> _dailyStatistics = new();

    [ObservableProperty]
    private ObservableCollection<StatusSummary> _statusBreakdown = new();

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    public DashboardViewModel(
        IUnitOfWork unitOfWork,
        ILogger<DashboardViewModel> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dashboard
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Loads all dashboard data
    /// </summary>
    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            await LoadInvoiceStatisticsAsync();
            await LoadReceiptStatisticsAsync();
            await LoadRevenueStatisticsAsync();
            await LoadDailyTrendsAsync();
            await LoadStatusBreakdownAsync();

        }, "Loading dashboard...");
    }

    /// <summary>
    /// Loads invoice statistics
    /// </summary>
    private async Task LoadInvoiceStatisticsAsync()
    {
        var allInvoices = await _unitOfWork.Invoices.GetAllAsync();
        TotalInvoices = allInvoices.Count();

        var today = DateTime.Today;
        var todayInvoices = allInvoices.Where(i => i.DateTimeIssued.Date == today);

        SubmittedToday = todayInvoices.Count(i =>
            i.Status == DocumentStatus.Submitted ||
            i.Status == DocumentStatus.Accepted);

        PendingSubmission = allInvoices.Count(i =>
            i.Status == DocumentStatus.Draft ||
            i.Status == DocumentStatus.Valid);

        AcceptedCount = allInvoices.Count(i => i.Status == DocumentStatus.Accepted);
        RejectedCount = allInvoices.Count(i => i.Status == DocumentStatus.Rejected);
    }

    /// <summary>
    /// Loads receipt statistics
    /// </summary>
    private async Task LoadReceiptStatisticsAsync()
    {
        var allReceipts = await _unitOfWork.Receipts.GetAllAsync();
        TotalReceipts = allReceipts.Count();
    }

    /// <summary>
    /// Loads revenue statistics
    /// </summary>
    private async Task LoadRevenueStatisticsAsync()
    {
        var allInvoices = await _unitOfWork.Invoices.GetAllAsync();
        var allReceipts = await _unitOfWork.Receipts.GetAllAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Today's revenue
        var todayInvoices = allInvoices.Where(i => i.DateTimeIssued.Date == today);
        var todayReceipts = allReceipts.Where(r => r.DateTimeIssued.Date == today);

        TotalRevenueToday = todayInvoices.Sum(i => i.TotalAmount) +
                           todayReceipts.Sum(r => r.TotalAmount);

        // This month's revenue
        var monthInvoices = allInvoices.Where(i => i.DateTimeIssued >= monthStart);
        var monthReceipts = allReceipts.Where(r => r.DateTimeIssued >= monthStart);

        TotalRevenueMonth = monthInvoices.Sum(i => i.TotalAmount) +
                           monthReceipts.Sum(r => r.TotalAmount);

        // Total tax collected
        TotalTaxCollected = monthInvoices.Sum(i => i.TotalTaxAmount) +
                           monthReceipts.Sum(r => r.TotalTaxAmount);
    }

    /// <summary>
    /// Loads daily trends for the last 30 days
    /// </summary>
    private async Task LoadDailyTrendsAsync()
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-29); // Last 30 days

        var invoices = await _unitOfWork.Invoices.GetByDateRangeAsync(startDate, endDate.AddDays(1));
        var receipts = await _unitOfWork.Receipts.GetByDateRangeAsync(startDate, endDate.AddDays(1));

        var dailyStats = new List<DailyStats>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayInvoices = invoices.Where(i => i.DateTimeIssued.Date == date);
            var dayReceipts = receipts.Where(r => r.DateTimeIssued.Date == date);

            dailyStats.Add(new DailyStats
            {
                Date = date,
                InvoiceCount = dayInvoices.Count(),
                ReceiptCount = dayReceipts.Count(),
                TotalRevenue = dayInvoices.Sum(i => i.TotalAmount) + dayReceipts.Sum(r => r.TotalAmount),
                TotalTax = dayInvoices.Sum(i => i.TotalTaxAmount) + dayReceipts.Sum(r => r.TotalTaxAmount)
            });
        }

        DailyStatistics = new ObservableCollection<DailyStats>(dailyStats);
    }

    /// <summary>
    /// Loads status breakdown
    /// </summary>
    private async Task LoadStatusBreakdownAsync()
    {
        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endDate = DateTime.Today.AddDays(1);

        var stats = await _unitOfWork.Invoices.GetSubmissionStatsAsync(startDate, endDate);

        var breakdown = new List<StatusSummary>
        {
            new() { Status = "Draft", Count = stats.GetValueOrDefault(DocumentStatus.Draft, 0), Color = "#FFA500" },
            new() { Status = "Valid", Count = stats.GetValueOrDefault(DocumentStatus.Valid, 0), Color = "#2196F3" },
            new() { Status = "Submitted", Count = stats.GetValueOrDefault(DocumentStatus.Submitted, 0), Color = "#9C27B0" },
            new() { Status = "Accepted", Count = stats.GetValueOrDefault(DocumentStatus.Accepted, 0), Color = "#4CAF50" },
            new() { Status = "Rejected", Count = stats.GetValueOrDefault(DocumentStatus.Rejected, 0), Color = "#F44336" },
        };

        StatusBreakdown = new ObservableCollection<StatusSummary>(breakdown);
    }

    /// <summary>
    /// Refreshes the dashboard
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDashboardDataAsync();
        _logger.LogInformation("Dashboard refreshed");
    }
}

/// <summary>
/// Daily statistics model
/// </summary>
public class DailyStats
{
    public DateTime Date { get; set; }
    public int InvoiceCount { get; set; }
    public int ReceiptCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalTax { get; set; }

    public string DateString => Date.ToString("MMM dd");
}

/// <summary>
/// Status summary model
/// </summary>
public class StatusSummary
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = string.Empty;
    public double Percentage { get; set; }
}
