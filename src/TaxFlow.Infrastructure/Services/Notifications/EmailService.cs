using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Notifications;

/// <summary>
/// Email service for sending notifications
/// </summary>
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<bool> SendInvoiceSubmissionNotificationAsync(string to, string invoiceNumber, bool success);
    Task<bool> SendBatchProcessingReportAsync(string to, int total, int success, int failed);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _smtpHost = _configuration["SMTP:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["SMTP:Port"] ?? "587");
        _smtpUsername = _configuration["SMTP:Username"] ?? "";
        _smtpPassword = _configuration["SMTP:Password"] ?? "";
        _fromEmail = _configuration["SMTP:FromEmail"] ?? "noreply@taxflow.com";
        _fromName = _configuration["SMTP:FromName"] ?? "TaxFlow Enterprise";
        _enableSsl = bool.Parse(_configuration["SMTP:EnableSSL"] ?? "true");
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail, _fromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort);
            smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            smtpClient.EnableSsl = _enableSsl;

            await smtpClient.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendInvoiceSubmissionNotificationAsync(string to, string invoiceNumber, bool success)
    {
        var subject = success
            ? $"Invoice {invoiceNumber} Submitted Successfully"
            : $"Invoice {invoiceNumber} Submission Failed";

        var body = success
            ? GenerateSuccessEmailBody(invoiceNumber)
            : GenerateFailureEmailBody(invoiceNumber);

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendBatchProcessingReportAsync(string to, int total, int success, int failed)
    {
        var subject = $"Batch Processing Report - {total} Invoices Processed";
        var body = GenerateBatchReportEmailBody(total, success, failed);

        return await SendEmailAsync(to, subject, body);
    }

    private string GenerateSuccessEmailBody(string invoiceNumber)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ text-align: center; padding: 10px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Invoice Submitted Successfully</h1>
        </div>
        <div class='content'>
            <p>Dear User,</p>
            <p>Your invoice <strong>{invoiceNumber}</strong> has been successfully submitted to the Egyptian Tax Authority (ETA).</p>
            <p><strong>Submission Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            <p>You can view the invoice details in the TaxFlow application.</p>
            <p>Thank you for using TaxFlow Enterprise.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 TaxFlow Enterprise. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateFailureEmailBody(string invoiceNumber)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ text-align: center; padding: 10px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✗ Invoice Submission Failed</h1>
        </div>
        <div class='content'>
            <p>Dear User,</p>
            <p>Your invoice <strong>{invoiceNumber}</strong> submission to the Egyptian Tax Authority (ETA) has failed.</p>
            <p><strong>Failed Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            <p>Please review the invoice details and try again. If the problem persists, contact support.</p>
            <p>Thank you for using TaxFlow Enterprise.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 TaxFlow Enterprise. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateBatchReportEmailBody(int total, int success, int failed)
    {
        var successRate = total > 0 ? (success * 100.0 / total) : 0;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .stats {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .stat {{ text-align: center; padding: 15px; background: white; border-radius: 5px; }}
        .stat-value {{ font-size: 32px; font-weight: bold; }}
        .stat-label {{ color: #666; }}
        .footer {{ text-align: center; padding: 10px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Batch Processing Report</h1>
        </div>
        <div class='content'>
            <p>Dear User,</p>
            <p>Your batch processing has been completed. Here are the results:</p>
            <div class='stats'>
                <div class='stat'>
                    <div class='stat-value'>{total}</div>
                    <div class='stat-label'>Total Invoices</div>
                </div>
                <div class='stat' style='color: #4CAF50;'>
                    <div class='stat-value'>{success}</div>
                    <div class='stat-label'>Successful</div>
                </div>
                <div class='stat' style='color: #f44336;'>
                    <div class='stat-value'>{failed}</div>
                    <div class='stat-label'>Failed</div>
                </div>
            </div>
            <p><strong>Success Rate:</strong> {successRate:F2}%</p>
            <p><strong>Completed Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            <p>For detailed results, please check the TaxFlow application.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 TaxFlow Enterprise. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
