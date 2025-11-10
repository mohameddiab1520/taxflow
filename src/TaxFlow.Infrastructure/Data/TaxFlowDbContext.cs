using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using System.Text.Json;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Infrastructure.Data;

/// <summary>
/// SQLite database context for real-time operational data
/// </summary>
public class TaxFlowDbContext : DbContext
{
    public TaxFlowDbContext(DbContextOptions<TaxFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<BatchHistory> BatchHistories => Set<BatchHistory>();
    public DbSet<BatchItemHistory> BatchItemHistories => Set<BatchItemHistory>();

    // Authentication & Authorization entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DateTimeIssued);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.EtaSubmissionId);

            entity.Property(e => e.TaxTotals)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TaxItem>>(v, (JsonSerializerOptions?)null) ?? new List<TaxItem>()
                );

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure InvoiceLine entity
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);

            entity.Property(e => e.TaxItems)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TaxItem>>(v, (JsonSerializerOptions?)null) ?? new List<TaxItem>()
                );

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Receipt entity
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DateTimeIssued);
            entity.HasIndex(e => e.TerminalId);

            entity.Property(e => e.TaxTotals)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TaxItem>>(v, (JsonSerializerOptions?)null) ?? new List<TaxItem>()
                );

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Receipts)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Lines)
                .WithOne(l => l.Receipt)
                .HasForeignKey(l => l.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure ReceiptLine entity
        modelBuilder.Entity<ReceiptLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptId);

            entity.Property(e => e.TaxItems)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TaxItem>>(v, (JsonSerializerOptions?)null) ?? new List<TaxItem>()
                );

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaxRegistrationNumber);
            entity.HasIndex(e => e.CommercialRegistrationNumber);

            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Country).HasMaxLength(2);
                address.Property(a => a.Governate).HasMaxLength(100);
                address.Property(a => a.RegionCity).HasMaxLength(100);
                address.Property(a => a.Street).HasMaxLength(200);
                address.Property(a => a.BuildingNumber).HasMaxLength(50);
                address.Property(a => a.PostalCode).HasMaxLength(20);
                address.Property(a => a.Floor).HasMaxLength(10);
                address.Property(a => a.Room).HasMaxLength(10);
                address.Property(a => a.Landmark).HasMaxLength(200);
                address.Property(a => a.AdditionalInformation).HasMaxLength(500);
            });

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure BatchHistory entity
        modelBuilder.Entity<BatchHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BatchType);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => new { e.BatchType, e.Status });
            entity.HasIndex(e => new { e.StartedAt, e.Status });

            entity.Property(e => e.BatchType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(100);

            entity.HasMany(e => e.ItemHistories)
                .WithOne(i => i.BatchHistory)
                .HasForeignKey(i => i.BatchHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure BatchItemHistory entity
        modelBuilder.Entity<BatchItemHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BatchHistoryId);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.IsSuccess);
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => new { e.BatchHistoryId, e.IsSuccess });

            entity.Property(e => e.DocumentNumber).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.EtaLongId).HasMaxLength(200);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FullNameAr).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FullNameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.SecurityStamp).HasMaxLength(500);
            entity.Property(e => e.LastLoginIp).HasMaxLength(50);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameAr).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Module);

            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameAr).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure UserRole (many-to-many)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.AssignedAt);
        });

        // Configure RolePermission (many-to-many)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.AssignedAt);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });

            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
