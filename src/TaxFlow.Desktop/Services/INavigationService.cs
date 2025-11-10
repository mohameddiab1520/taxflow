using TaxFlow.Desktop.ViewModels;

namespace TaxFlow.Desktop.Services;

/// <summary>
/// Navigation service interface for view model navigation
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Event raised when the current view model changes
    /// </summary>
    event Action<ViewModelBase?>? CurrentViewModelChanged;

    /// <summary>
    /// Gets the current view model
    /// </summary>
    ViewModelBase? CurrentViewModel { get; }

    /// <summary>
    /// Navigates to a view model
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>
    /// Navigates back to the previous view model
    /// </summary>
    void GoBack();

    /// <summary>
    /// Checks if navigation back is possible
    /// </summary>
    bool CanGoBack { get; }
}
