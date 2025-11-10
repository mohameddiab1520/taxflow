using CommunityToolkit.Mvvm.ComponentModel;

namespace TaxFlow.Desktop.ViewModels;

/// <summary>
/// Base view model class for all view models
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string _busyMessage = string.Empty;
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Indicates if the view model is performing an operation
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Message to display while busy
    /// </summary>
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>
    /// Error message to display
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Clears the error message
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Sets an error message
    /// </summary>
    public void SetError(string message)
    {
        ErrorMessage = message;
    }

    /// <summary>
    /// Executes an async operation with busy state
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, string? busyMessage = null)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        BusyMessage = busyMessage ?? "Processing...";
        ClearError();

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
}
