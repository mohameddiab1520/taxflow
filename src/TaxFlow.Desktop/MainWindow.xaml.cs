using MahApps.Metro.Controls;
using TaxFlow.Desktop.ViewModels;

namespace TaxFlow.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Navigate to dashboard on startup
        viewModel.NavigateToDashboardCommand.Execute(null);
    }
}
