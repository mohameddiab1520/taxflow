using Microsoft.EntityFrameworkCore;
using TaxFlow.Core.Entities;
using System.Text.Json;
using TaxFlow.Core.ValueObjects;

namespace TaxFlow.Infrastructure.Data;

/// <summary>
/// PostgreSQL database context for analytics and reporting
/// </summary>
public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use same configuration as SQLite but optimized for PostgreSQL
        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DateTimeIssued);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt); // For time-series analytics

            // PostgreSQL supports JSONB for better performance
            entity.Property(e => e.TaxTotals)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TaxItem>>(v, (JsonSerializerOptions?)null) ?? new List<TaxItem>()
                );

            entity.Property(e => e.JsonDocument)
                .HasColumnType("jsonb");

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
            entity.HasIndex(e => e.ItemCode);

            entity.Property(e => e.TaxItems)
                .HasColumnType("jsonb")
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
            entity.HasIndex(e => e.ReceiptNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DateTimeIssued);
            entity.HasIndex(e => e.TerminalId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.TaxTotals)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<TaxItem>>(v, (JsonSerializerOptions?)null) ?? new List<TaxItem>()
                );

            entity.Property(e => e.JsonDocument)
                .HasColumnType("jsonb");

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
            entity.HasIndex(e => e.ItemCode);

            entity.Property(e => e.TaxItems)
                .HasColumnType("jsonb")
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
    }
}
