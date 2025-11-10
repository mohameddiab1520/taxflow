namespace TaxFlow.Core.Enums;

/// <summary>
/// Predefined system permissions
/// </summary>
public static class PermissionCodes
{
    // Invoice permissions
    public const string InvoiceView = "invoice.view";
    public const string InvoiceCreate = "invoice.create";
    public const string InvoiceEdit = "invoice.edit";
    public const string InvoiceDelete = "invoice.delete";
    public const string InvoiceSubmit = "invoice.submit";
    public const string InvoiceCancel = "invoice.cancel";

    // Receipt permissions
    public const string ReceiptView = "receipt.view";
    public const string ReceiptCreate = "receipt.create";
    public const string ReceiptEdit = "receipt.edit";
    public const string ReceiptDelete = "receipt.delete";
    public const string ReceiptSubmit = "receipt.submit";

    // Customer permissions
    public const string CustomerView = "customer.view";
    public const string CustomerCreate = "customer.create";
    public const string CustomerEdit = "customer.edit";
    public const string CustomerDelete = "customer.delete";

    // Certificate permissions
    public const string CertificateView = "certificate.view";
    public const string CertificateInstall = "certificate.install";
    public const string CertificateExport = "certificate.export";
    public const string CertificateDelete = "certificate.delete";

    // Batch processing permissions
    public const string BatchSubmit = "batch.submit";
    public const string BatchMonitor = "batch.monitor";
    public const string BatchCancel = "batch.cancel";

    // Report permissions
    public const string ReportView = "report.view";
    public const string ReportExport = "report.export";
    public const string ReportPrint = "report.print";

    // User management permissions
    public const string UserView = "user.view";
    public const string UserCreate = "user.create";
    public const string UserEdit = "user.edit";
    public const string UserDelete = "user.delete";
    public const string UserResetPassword = "user.reset_password";

    // Role management permissions
    public const string RoleView = "role.view";
    public const string RoleCreate = "role.create";
    public const string RoleEdit = "role.edit";
    public const string RoleDelete = "role.delete";
    public const string RoleAssignPermissions = "role.assign_permissions";

    // System settings permissions
    public const string SettingsView = "settings.view";
    public const string SettingsEdit = "settings.edit";
    public const string SettingsBackup = "settings.backup";
    public const string SettingsRestore = "settings.restore";

    // Audit permissions
    public const string AuditView = "audit.view";
    public const string AuditExport = "audit.export";
}

/// <summary>
/// Predefined system roles
/// </summary>
public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Accountant = "Accountant";
    public const string Cashier = "Cashier";
    public const string Viewer = "Viewer";
}
