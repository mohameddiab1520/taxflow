using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TaxFlow.Core.Entities;
using TaxFlow.Core.ValueObjects;
using TaxFlow.Core.Enums;
using TaxFlow.Infrastructure.Services.Tax;
using TaxFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Tests.Performance;

/// <summary>
/// Performance benchmarks for TaxFlow system
/// Run with: dotnet run -c Release
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<PerformanceTests>();
        Console.WriteLine(summary);
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class PerformanceTests
{
    private TaxCalculationService _taxService = null!;
    private Invoice _sampleInvoice = null!;
    private List<Invoice> _batchInvoices = null!;
    private TaxFlowDbContext _dbContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup tax calculation service
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<TaxCalculationService>();
        _taxService = new TaxCalculationService(logger);

        // Setup sample invoice for tax calculation
        _sampleInvoice = CreateSampleInvoice();

        // Setup batch of invoices
        _batchInvoices = CreateBatchInvoices(10000);

        // Setup in-memory database for query tests
        var options = new DbContextOptionsBuilder<TaxFlowDbContext>()
            .UseInMemoryDatabase(databaseName: "PerformanceTestDb")
            .Options;

        _dbContext = new TaxFlowDbContext(options);

        // Seed database with test data
        SeedDatabase();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext?.Dispose();
    }

    /// <summary>
    /// Benchmark: Tax calculation performance for a single invoice
    /// </summary>
    [Benchmark]
    public async Task TaxCalculationPerformanceTest()
    {
        var result = await _taxService.CalculateTaxesAsync(_sampleInvoice);
    }

    /// <summary>
    /// Benchmark: Tax calculation for multiple invoice lines
    /// </summary>
    [Benchmark]
    public async Task TaxCalculationMultipleLines()
    {
        var invoice = CreateInvoiceWithMultipleLines(50);
        var result = await _taxService.CalculateTaxesAsync(invoice);
    }

    /// <summary>
    /// Benchmark: Batch processing of 1,000 invoices
    /// </summary>
    [Benchmark]
    public async Task BatchProcessing1000Invoices()
    {
        var tasks = new List<Task>();
        var batch = _batchInvoices.Take(1000).ToList();

        foreach (var invoice in batch)
        {
            tasks.Add(_taxService.CalculateTaxesAsync(invoice));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark: Batch processing of 10,000 invoices
    /// </summary>
    [Benchmark]
    public async Task BatchProcessing10000Invoices()
    {
        var tasks = new List<Task>();

        // Process in chunks to avoid overwhelming system
        foreach (var chunk in _batchInvoices.Chunk(100))
        {
            var chunkTasks = chunk.Select(invoice => _taxService.CalculateTaxesAsync(invoice));
            await Task.WhenAll(chunkTasks);
        }
    }

    /// <summary>
    /// Benchmark: Database query performance - Get all invoices
    /// </summary>
    [Benchmark]
    public async Task DatabaseQueryGetAllInvoices()
    {
        var invoices = await _dbContext.Invoices.ToListAsync();
    }

    /// <summary>
    /// Benchmark: Database query performance - Get invoices with filtering
    /// </summary>
    [Benchmark]
    public async Task DatabaseQueryWithFiltering()
    {
        var invoices = await _dbContext.Invoices
            .Where(i => i.Status == DocumentStatus.Submitted)
            .Where(i => i.DateTimeIssued >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(i => i.DateTimeIssued)
            .Take(100)
            .ToListAsync();
    }

    /// <summary>
    /// Benchmark: Database query performance - Get invoice with details (includes)
    /// </summary>
    [Benchmark]
    public async Task DatabaseQueryWithIncludes()
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Benchmark: Database query performance - Complex aggregation
    /// </summary>
    [Benchmark]
    public async Task DatabaseQueryComplexAggregation()
    {
        var stats = await _dbContext.Invoices
            .GroupBy(i => i.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(i => i.TotalAmount),
                AverageAmount = g.Average(i => i.TotalAmount)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Benchmark: Invoice serialization to JSON
    /// </summary>
    [Benchmark]
    public void InvoiceSerializationPerformance()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_sampleInvoice);
    }

    /// <summary>
    /// Benchmark: Invoice deserialization from JSON
    /// </summary>
    [Benchmark]
    public void InvoiceDeserializationPerformance()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_sampleInvoice);
        var invoice = System.Text.Json.JsonSerializer.Deserialize<Invoice>(json);
    }

    /// <summary>
    /// Benchmark: Parallel processing of invoices
    /// </summary>
    [Benchmark]
    public async Task ParallelProcessing()
    {
        var batch = _batchInvoices.Take(1000).ToList();

        await Parallel.ForEachAsync(
            batch,
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (invoice, ct) =>
            {
                await _taxService.CalculateTaxesAsync(invoice);
            });
    }

    /// <summary>
    /// Benchmark: Sequential processing of invoices
    /// </summary>
    [Benchmark]
    public async Task SequentialProcessing()
    {
        var batch = _batchInvoices.Take(100).ToList();

        foreach (var invoice in batch)
        {
            await _taxService.CalculateTaxesAsync(invoice);
        }
    }

    #region Helper Methods

    private Invoice CreateSampleInvoice()
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            DocumentType = DocumentType.Invoice,
            DocumentTypeVersion = "1.0",
            DateTimeIssued = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            Status = DocumentStatus.Draft,
            TaxTotals = new List<TaxItem>()
        };

        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            DescriptionAr = "منتج 1",
            DescriptionEn = "Product 1",
            ItemCode = "ITEM-001",
            UnitType = "EA",
            Quantity = 10,
            UnitPrice = 100,
            TaxItems = new List<TaxItem>(),
            TotalAmount = 1000,
            NetAmount = 1000
        });

        return invoice;
    }

    private Invoice CreateInvoiceWithMultipleLines(int lineCount)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{Guid.NewGuid().ToString().Substring(0, 8)}",
            DocumentType = DocumentType.Invoice,
            DocumentTypeVersion = "1.0",
            DateTimeIssued = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            Status = DocumentStatus.Draft,
            TaxTotals = new List<TaxItem>()
        };

        for (int i = 0; i < lineCount; i++)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                DescriptionAr = $"منتج {i + 1}",
                DescriptionEn = $"Product {i + 1}",
                ItemCode = $"ITEM-{i + 1:000}",
                UnitType = "EA",
                Quantity = 1 + (i % 10),
                UnitPrice = 100 + (i * 10),
                TaxItems = new List<TaxItem>(),
                TotalAmount = (1 + (i % 10)) * (100 + (i * 10)),
                NetAmount = (1 + (i % 10)) * (100 + (i * 10))
            });
        }

        return invoice;
    }

    private List<Invoice> CreateBatchInvoices(int count)
    {
        var invoices = new List<Invoice>();

        for (int i = 0; i < count; i++)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{i + 1:00000}",
                DocumentType = DocumentType.Invoice,
                DocumentTypeVersion = "1.0",
                DateTimeIssued = DateTime.UtcNow.AddDays(-i % 365),
                CustomerId = Guid.NewGuid(),
                Status = (DocumentStatus)(i % 5),
                TaxTotals = new List<TaxItem>()
            };

            // Add 1-5 lines per invoice
            var lineCount = 1 + (i % 5);
            for (int j = 0; j < lineCount; j++)
            {
                invoice.Lines.Add(new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    DescriptionAr = $"منتج {j + 1}",
                    DescriptionEn = $"Product {j + 1}",
                    ItemCode = $"ITEM-{j + 1:000}",
                    UnitType = "EA",
                    Quantity = 1 + (j % 10),
                    UnitPrice = 100 + (j * 10),
                    TaxItems = new List<TaxItem>(),
                    TotalAmount = (1 + (j % 10)) * (100 + (j * 10)),
                    NetAmount = (1 + (j % 10)) * (100 + (j * 10))
                });
            }

            invoices.Add(invoice);
        }

        return invoices;
    }

    private void SeedDatabase()
    {
        // Seed customers
        var customers = new List<Customer>();
        for (int i = 0; i < 100; i++)
        {
            customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                NameAr = $"عميل {i + 1}",
                NameEn = $"Customer {i + 1}",
                TaxRegistrationNumber = $"TRN{i + 1:000}",
                CustomerType = "B",
                Address = new CustomerAddress
                {
                    Country = "EG",
                    Governate = "Cairo",
                    RegionCity = "Cairo",
                    Street = $"Street {i + 1}",
                    BuildingNumber = $"{i + 1}"
                }
            });
        }

        _dbContext.Customers.AddRange(customers);
        _dbContext.SaveChanges();

        // Seed invoices
        var invoices = new List<Invoice>();
        for (int i = 0; i < 1000; i++)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{i + 1:00000}",
                DocumentType = DocumentType.Invoice,
                DocumentTypeVersion = "1.0",
                DateTimeIssued = DateTime.UtcNow.AddDays(-i % 365),
                CustomerId = customers[i % customers.Count].Id,
                Status = (DocumentStatus)(i % 5),
                TotalAmount = 1000 + (i * 10),
                TaxTotals = new List<TaxItem>()
            };

            invoices.Add(invoice);
        }

        _dbContext.Invoices.AddRange(invoices);
        _dbContext.SaveChanges();
    }

    #endregion
}
