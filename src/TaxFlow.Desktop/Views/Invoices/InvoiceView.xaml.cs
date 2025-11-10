using System.Windows.Controls;
using TaxFlow.Desktop.ViewModels.Invoices;

namespace TaxFlow.Desktop.Views.Invoices;

/// <summary>
/// Interaction logic for InvoiceView.xaml
/// </summary>
public partial class InvoiceView : UserControl
{
    public InvoiceView()
    {
        InitializeComponent();
    }

    public InvoiceView(InvoiceViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
