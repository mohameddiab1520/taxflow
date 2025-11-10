using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace TaxFlow.Infrastructure.Data;

/// <summary>
/// Seeds initial data for users, roles, and permissions
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(TaxFlowDbContext context)
    {
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
    }

    private static async Task SeedPermissionsAsync(TaxFlowDbContext context)
    {
        if (await context.Set<Permission>().AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            // Invoice permissions
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.InvoiceView, Name = "View Invoices", NameAr = "عرض الفواتير", Module = "Invoice", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.InvoiceCreate, Name = "Create Invoice", NameAr = "إنشاء فاتورة", Module = "Invoice", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.InvoiceEdit, Name = "Edit Invoice", NameAr = "تعديل فاتورة", Module = "Invoice", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.InvoiceDelete, Name = "Delete Invoice", NameAr = "حذف فاتورة", Module = "Invoice", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.InvoiceSubmit, Name = "Submit Invoice", NameAr = "إرسال فاتورة", Module = "Invoice", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.InvoiceCancel, Name = "Cancel Invoice", NameAr = "إلغاء فاتورة", Module = "Invoice", IsSystemPermission = true },

            // Receipt permissions
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReceiptView, Name = "View Receipts", NameAr = "عرض الإيصالات", Module = "Receipt", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReceiptCreate, Name = "Create Receipt", NameAr = "إنشاء إيصال", Module = "Receipt", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReceiptEdit, Name = "Edit Receipt", NameAr = "تعديل إيصال", Module = "Receipt", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReceiptDelete, Name = "Delete Receipt", NameAr = "حذف إيصال", Module = "Receipt", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReceiptSubmit, Name = "Submit Receipt", NameAr = "إرسال إيصال", Module = "Receipt", IsSystemPermission = true },

            // Customer permissions
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.CustomerView, Name = "View Customers", NameAr = "عرض العملاء", Module = "Customer", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.CustomerCreate, Name = "Create Customer", NameAr = "إنشاء عميل", Module = "Customer", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.CustomerEdit, Name = "Edit Customer", NameAr = "تعديل عميل", Module = "Customer", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.CustomerDelete, Name = "Delete Customer", NameAr = "حذف عميل", Module = "Customer", IsSystemPermission = true },

            // User management
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.UserView, Name = "View Users", NameAr = "عرض المستخدمين", Module = "User", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.UserCreate, Name = "Create User", NameAr = "إنشاء مستخدم", Module = "User", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.UserEdit, Name = "Edit User", NameAr = "تعديل مستخدم", Module = "User", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.UserDelete, Name = "Delete User", NameAr = "حذف مستخدم", Module = "User", IsSystemPermission = true },

            // Reports
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReportView, Name = "View Reports", NameAr = "عرض التقارير", Module = "Report", IsSystemPermission = true },
            new() { Id = Guid.NewGuid(), Code = PermissionCodes.ReportExport, Name = "Export Reports", NameAr = "تصدير التقارير", Module = "Report", IsSystemPermission = true },
        };

        await context.Set<Permission>().AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(TaxFlowDbContext context)
    {
        if (await context.Set<Role>().AnyAsync())
            return;

        var permissions = await context.Set<Permission>().ToListAsync();

        // Administrator role - all permissions
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = SystemRoles.Administrator,
            NameAr = "مدير النظام",
            Description = "Full system access",
            IsSystemRole = true
        };

        foreach (var permission in permissions)
        {
            adminRole.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // Manager role - most permissions except user management
        var managerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = SystemRoles.Manager,
            NameAr = "مدير",
            Description = "Manager access",
            IsSystemRole = true
        };

        var managerPermissions = permissions.Where(p => !p.Code.StartsWith("user.")).ToList();
        foreach (var permission in managerPermissions)
        {
            managerRole.RolePermissions.Add(new RolePermission
            {
                RoleId = managerRole.Id,
                PermissionId = permission.Id
            });
        }

        // Accountant role - invoice and report access
        var accountantRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = SystemRoles.Accountant,
            NameAr = "محاسب",
            Description = "Accounting access",
            IsSystemRole = true
        };

        var accountantPermissions = permissions
            .Where(p => p.Module == "Invoice" || p.Module == "Customer" || p.Module == "Report")
            .ToList();
        foreach (var permission in accountantPermissions)
        {
            accountantRole.RolePermissions.Add(new RolePermission
            {
                RoleId = accountantRole.Id,
                PermissionId = permission.Id
            });
        }

        // Cashier role - receipt access only
        var cashierRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = SystemRoles.Cashier,
            NameAr = "كاشير",
            Description = "Cashier access",
            IsSystemRole = true
        };

        var cashierPermissions = permissions.Where(p => p.Module == "Receipt").ToList();
        foreach (var permission in cashierPermissions)
        {
            cashierRole.RolePermissions.Add(new RolePermission
            {
                RoleId = cashierRole.Id,
                PermissionId = permission.Id
            });
        }

        await context.Set<Role>().AddRangeAsync(new[] { adminRole, managerRole, accountantRole, cashierRole });
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(TaxFlowDbContext context)
    {
        if (await context.Set<User>().AnyAsync())
            return;

        var adminRole = await context.Set<Role>().FirstOrDefaultAsync(r => r.Name == SystemRoles.Administrator);
        if (adminRole == null)
            return;

        // Create default admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@taxflow.com",
            PasswordHash = HashPassword("Admin@123"),
            FullNameAr = "المدير",
            FullNameEn = "Administrator",
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        adminUser.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        });

        await context.Set<User>().AddAsync(adminUser);
        await context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
