using System.Windows.Controls;
using TaxFlow.Desktop.ViewModels.Settings;

namespace TaxFlow.Desktop.Views.Settings;

/// <summary>
/// Interaction logic for BatchSubmissionView.xaml
/// </summary>
public partial class BatchSubmissionView : UserControl
{
    public BatchSubmissionView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is BatchSubmissionViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
