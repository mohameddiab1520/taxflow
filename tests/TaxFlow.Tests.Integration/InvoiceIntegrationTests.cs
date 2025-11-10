using Xunit;
using Microsoft.EntityFrameworkCore;
using TaxFlow.Infrastructure.Data;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Infrastructure.Repositories;

namespace TaxFlow.Tests.Integration;

public class InvoiceIntegrationTests : IDisposable
{
    private readonly TaxFlowDbContext _context;
    private readonly InvoiceRepository _repository;

    public InvoiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<TaxFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TaxFlowDbContext(options);
        _repository = new InvoiceRepository(_context);
    }

    [Fact]
    public async Task AddInvoice_WithLines_SavesSuccessfully()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-TEST-001",
            DateTimeIssued = DateTime.UtcNow,
            DocumentType = DocumentType.Invoice,
            Status = DocumentStatus.Draft,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Test Product",
                    Quantity = 10,
                    UnitPrice = 100
                }
            }
        };

        // Act
        await _repository.AddAsync(invoice);
        await _context.SaveChangesAsync();

        // Assert
        var savedInvoice = await _repository.GetWithDetailsAsync(invoice.Id);
        Assert.NotNull(savedInvoice);
        Assert.Equal("INV-TEST-001", savedInvoice.InvoiceNumber);
        Assert.Single(savedInvoice.Lines);
    }

    [Fact]
    public async Task GetPendingInvoices_ReturnsOnlyPendingStatus()
    {
        // Arrange
        await _repository.AddAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            Status = DocumentStatus.Draft
        });

        await _repository.AddAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-002",
            Status = DocumentStatus.Submitted
        });

        await _context.SaveChangesAsync();

        // Act
        var pending = await _repository.GetPendingSubmissionAsync();

        // Assert
        Assert.Single(pending);
        Assert.Equal(DocumentStatus.Draft, pending.First().Status);
    }

    [Fact]
    public async Task UpdateInvoiceStatus_ChangesStatus()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-003",
            Status = DocumentStatus.Draft
        };

        await _repository.AddAsync(invoice);
        await _context.SaveChangesAsync();

        // Act
        invoice.Status = DocumentStatus.Submitted;
        invoice.SubmittedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(invoice);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(invoice.Id);
        Assert.Equal(DocumentStatus.Submitted, updated!.Status);
        Assert.NotNull(updated.SubmittedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
