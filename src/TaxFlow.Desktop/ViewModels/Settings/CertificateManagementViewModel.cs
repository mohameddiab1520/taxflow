using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace TaxFlow.Desktop.ViewModels.Settings;

/// <summary>
/// View model for certificate management
/// </summary>
public partial class CertificateManagementViewModel : ViewModelBase
{
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificateManagementViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<CertificateInfo> _certificates = new();

    [ObservableProperty]
    private CertificateInfo? _selectedCertificate;

    [ObservableProperty]
    private string? _selectedThumbprint;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private int _totalCertificates;

    [ObservableProperty]
    private int _validCertificates;

    [ObservableProperty]
    private int _expiredCertificates;

    [ObservableProperty]
    private int _expiringCertificates;

    public CertificateManagementViewModel(
        ICertificateService certificateService,
        ILogger<CertificateManagementViewModel> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCertificatesAsync();
    }

    /// <summary>
    /// Loads all available certificates
    /// </summary>
    [RelayCommand]
    private async Task LoadCertificatesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var certs = await _certificateService.GetAvailableCertificatesAsync();
            Certificates = new ObservableCollection<CertificateInfo>(certs);

            TotalCertificates = Certificates.Count;
            ValidCertificates = Certificates.Count(c => c.IsValid);
            ExpiredCertificates = Certificates.Count(c => c.IsExpired);
            ExpiringCertificates = Certificates.Count(c => !c.IsExpired && c.DaysUntilExpiry <= 30);

            _logger.LogInformation(
                "Loaded {Total} certificates ({Valid} valid, {Expired} expired, {Expiring} expiring soon)",
                TotalCertificates,
                ValidCertificates,
                ExpiredCertificates,
                ExpiringCertificates);

        }, "Loading certificates...");
    }

    /// <summary>
    /// Validates the selected certificate
    /// </summary>
    [RelayCommand]
    private async Task ValidateCertificateAsync()
    {
        if (SelectedCertificate == null)
        {
            ValidationMessage = "Please select a certificate";
            IsValid = false;
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _certificateService.ValidateCertificateAsync(
                SelectedCertificate.Thumbprint);

            IsValid = result.IsValid;

            if (result.IsValid)
            {
                ValidationMessage = "✓ Certificate is valid and ready for use";

                if (result.Warnings.Any())
                {
                    ValidationMessage += "\n\nWarnings:\n" + string.Join("\n", result.Warnings.Select(w => $"• {w}"));
                }
            }
            else
            {
                ValidationMessage = "✗ Certificate validation failed:\n\n" +
                    string.Join("\n", result.Errors.Select(e => $"• {e}"));
            }

            _logger.LogInformation(
                "Certificate {Thumbprint} validation: {IsValid}",
                SelectedCertificate.Thumbprint,
                result.IsValid);

        }, "Validating certificate...");
    }

    /// <summary>
    /// Installs a certificate from PFX file
    /// </summary>
    [RelayCommand]
    private async Task InstallCertificateAsync()
    {
        await ExecuteAsync(async () =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Certificate Files (*.pfx;*.p12)|*.pfx;*.p12|All Files (*.*)|*.*",
                Title = "Select Certificate File"
            };

            if (dialog.ShowDialog() != true)
                return;

            // Prompt for password securely
            var password = await PromptForPasswordAsync("Enter certificate password:");

            if (password == null)
            {
                _logger.LogInformation("Certificate installation cancelled by user");
                return;
            }

            var success = await _certificateService.InstallCertificateAsync(
                dialog.FileName,
                password);

            if (success)
            {
                _logger.LogInformation("Certificate installed successfully from {Path}", dialog.FileName);
                await LoadCertificatesAsync();
            }
            else
            {
                SetError("Failed to install certificate. Please check the file and password.");
            }

        }, "Installing certificate...");
    }

    /// <summary>
    /// Exports the selected certificate to PFX file
    /// </summary>
    [RelayCommand]
    private async Task ExportCertificateAsync()
    {
        if (SelectedCertificate == null)
        {
            SetError("Please select a certificate to export");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Certificate Files (*.pfx)|*.pfx|All Files (*.*)|*.*",
                Title = "Export Certificate",
                FileName = $"certificate_{SelectedCertificate.SerialNumber}.pfx"
            };

            if (dialog.ShowDialog() != true)
                return;

            // Prompt for password securely
            var password = await PromptForPasswordAsync("Enter password to protect exported certificate:");

            if (password == null)
            {
                _logger.LogInformation("Certificate export cancelled by user");
                return;
            }

            var success = await _certificateService.ExportCertificateAsync(
                SelectedCertificate.Thumbprint,
                dialog.FileName,
                password);

            if (success)
            {
                _logger.LogInformation(
                    "Certificate exported successfully to {Path}",
                    dialog.FileName);
            }
            else
            {
                SetError("Failed to export certificate");
            }

        }, "Exporting certificate...");
    }

    /// <summary>
    /// Removes the selected certificate
    /// </summary>
    [RelayCommand]
    private async Task RemoveCertificateAsync()
    {
        if (SelectedCertificate == null)
        {
            SetError("Please select a certificate to remove");
            return;
        }

        // In production, show confirmation dialog
        await ExecuteAsync(async () =>
        {
            var success = await _certificateService.RemoveCertificateAsync(
                SelectedCertificate.Thumbprint);

            if (success)
            {
                _logger.LogInformation(
                    "Certificate {Subject} removed successfully",
                    SelectedCertificate.Subject);

                Certificates.Remove(SelectedCertificate);
                SelectedCertificate = null;
                TotalCertificates = Certificates.Count;
            }
            else
            {
                SetError("Failed to remove certificate");
            }

        }, "Removing certificate...");
    }

    /// <summary>
    /// Selects a certificate for use
    /// </summary>
    [RelayCommand]
    private void SelectCertificate(CertificateInfo certificate)
    {
        SelectedCertificate = certificate;
        SelectedThumbprint = certificate.Thumbprint;

        _logger.LogInformation(
            "Certificate selected: {Subject} (Valid until: {ValidTo:yyyy-MM-dd})",
            certificate.Subject,
            certificate.ValidTo);
    }

    /// <summary>
    /// Checks for expiring certificates
    /// </summary>
    [RelayCommand]
    private async Task CheckExpiringCertificatesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var expiring = Certificates
                .Where(c => !c.IsExpired && c.DaysUntilExpiry <= 30)
                .OrderBy(c => c.DaysUntilExpiry)
                .ToList();

            if (expiring.Any())
            {
                var message = "The following certificates are expiring soon:\n\n";
                foreach (var cert in expiring)
                {
                    message += $"• {cert.Subject}\n  Expires in {cert.DaysUntilExpiry} days ({cert.ValidTo:yyyy-MM-dd})\n\n";
                }

                ValidationMessage = message;
                _logger.LogWarning(
                    "{Count} certificate(s) expiring within 30 days",
                    expiring.Count);
            }
            else
            {
                ValidationMessage = "✓ No certificates are expiring within 30 days";
            }

        }, "Checking certificate expiration...");
    }

    /// <summary>
    /// Refreshes the certificate list
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadCertificatesAsync();
    }

    /// <summary>
    /// Property changed handler
    /// </summary>
    partial void OnSelectedCertificateChanged(CertificateInfo? value)
    {
        if (value != null)
        {
            _ = ValidateCertificateAsync();
        }
    }

    /// <summary>
    /// Prompts user for password using MahApps.Metro dialog
    /// </summary>
    private async Task<string?> PromptForPasswordAsync(string message)
    {
        try
        {
            // Get the main window for dialog context
            var mainWindow = Application.Current.MainWindow as MetroWindow;
            if (mainWindow == null)
            {
                // Fallback to simple dialog if MetroWindow is not available
                var passwordDialog = new System.Windows.Controls.PasswordBox();
                var result = MessageBox.Show(
                    $"{message}\nPlease enter password in the console or configuration.",
                    "Password Required",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

                return result == MessageBoxResult.OK ? string.Empty : null;
            }

            // Use MahApps.Metro LoginDialog for password input
            var loginDialogSettings = new LoginDialogSettings
            {
                InitialUsername = string.Empty,
                UsernameWatermark = "Not required",
                PasswordWatermark = "Certificate password",
                NegativeButtonText = "Cancel",
                AffirmativeButtonText = "OK",
                EnablePasswordPreview = true,
                RememberCheckBoxVisibility = System.Windows.Visibility.Collapsed
            };

            var dialogResult = await mainWindow.ShowLoginAsync(
                "Certificate Password",
                message,
                loginDialogSettings);

            return dialogResult != null ? dialogResult.Password : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing password dialog");
            return string.Empty; // Return empty password on error
        }
    }
}
