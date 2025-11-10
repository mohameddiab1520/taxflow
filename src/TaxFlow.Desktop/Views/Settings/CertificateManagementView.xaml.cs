using System.Windows.Controls;
using TaxFlow.Desktop.ViewModels.Settings;

namespace TaxFlow.Desktop.Views.Settings;

/// <summary>
/// Interaction logic for CertificateManagementView.xaml
/// </summary>
public partial class CertificateManagementView : UserControl
{
    public CertificateManagementView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CertificateManagementViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
