using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Desktop.Services;
using TaxFlow.Desktop.ViewModels.Invoices;
using TaxFlow.Desktop.ViewModels.Customers;

namespace TaxFlow.Desktop.ViewModels;

/// <summary>
/// View model for the main window
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string _title = "TaxFlow Enterprise - Egyptian Tax Invoice System";

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _currentLanguage = "en";

    public MainWindowViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _localizationService = localizationService;

        // Subscribe to navigation changes
        _navigationService.CurrentViewModelChanged += OnCurrentViewModelChanged;

        // Load theme and language preferences
        IsDarkTheme = _themeService.IsDarkTheme;
        CurrentLanguage = _localizationService.CurrentLanguage;
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    [RelayCommand]
    private void NavigateToInvoices()
    {
        _navigationService.NavigateTo<InvoiceListViewModel>();
    }

    [RelayCommand]
    private void NavigateToReceipts()
    {
        _navigationService.NavigateTo<ReceiptListViewModel>();
    }

    [RelayCommand]
    private void NavigateToCustomers()
    {
        _navigationService.NavigateTo<CustomerListViewModel>();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _themeService.SetTheme(IsDarkTheme);
    }

    [RelayCommand]
    private void SwitchLanguage(string language)
    {
        CurrentLanguage = language;
        _localizationService.SetLanguage(language);
        // Reload current view to apply language changes
        OnPropertyChanged(nameof(Title));
    }

    private void OnCurrentViewModelChanged(ViewModelBase? viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
