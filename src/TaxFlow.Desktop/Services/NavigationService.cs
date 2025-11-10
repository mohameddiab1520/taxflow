using Microsoft.Extensions.DependencyInjection;
using TaxFlow.Desktop.ViewModels;

namespace TaxFlow.Desktop.Services;

/// <summary>
/// Navigation service implementation
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<ViewModelBase> _navigationStack = new();
    private ViewModelBase? _currentViewModel;

    public event Action<ViewModelBase?>? CurrentViewModelChanged;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            CurrentViewModelChanged?.Invoke(_currentViewModel);
        }
    }

    public bool CanGoBack => _navigationStack.Count > 0;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        // Push current view model to stack
        if (CurrentViewModel != null)
        {
            _navigationStack.Push(CurrentViewModel);
        }

        // Create new view model instance
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
    }

    public void GoBack()
    {
        if (!CanGoBack)
            return;

        CurrentViewModel = _navigationStack.Pop();
    }
}
