using Xunit;
using Moq;
using TaxFlow.Infrastructure.Services.Processing;
using TaxFlow.Core.Interfaces;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Tests.Unit.Services;

public class BatchProcessingServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDigitalSignatureService> _mockSignatureService;
    private readonly Mock<IEtaSubmissionService> _mockEtaService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<BatchProcessingService>> _mockLogger;
    private readonly BatchProcessingService _service;

    public BatchProcessingServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSignatureService = new Mock<IDigitalSignatureService>();
        _mockEtaService = new Mock<IEtaSubmissionService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<BatchProcessingService>>();

        _service = new BatchProcessingService(
            _mockUnitOfWork.Object,
            _mockSignatureService.Object,
            _mockEtaService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ProcessInvoiceBatchAsync_EmptyList_ReturnsZeroProcessed()
    {
        // Arrange
        var invoiceIds = new List<Guid>();
        var options = new BatchProcessingOptions();

        // Act
        var result = await _service.ProcessInvoiceBatchAsync(invoiceIds, "cert-thumbprint", options);

        // Assert
        Assert.Equal(0, result.TotalDocuments);
        Assert.Equal(0, result.SuccessfullyProcessed);
    }

    [Fact]
    public async Task ProcessInvoiceBatchAsync_ValidInvoices_ProcessesSuccessfully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = "INV-001",
            Status = DocumentStatus.Valid
        };

        _mockUnitOfWork.Setup(x => x.Invoices.GetWithDetailsAsync(invoiceId))
            .ReturnsAsync(invoice);

        _mockSignatureService.Setup(x => x.SignJsonDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignatureResult { IsSuccess = true, SignatureValue = "signature" });

        _mockEtaService.Setup(x => x.SubmitInvoiceAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EtaSubmissionResult { IsSuccess = true, LongId = "ETA-123" });

        var options = new BatchProcessingOptions { MaxRetryAttempts = 1 };

        // Act
        var result = await _service.ProcessInvoiceBatchAsync(
            new List<Guid> { invoiceId },
            "cert-thumbprint",
            options);

        // Assert
        Assert.Equal(1, result.TotalDocuments);
        Assert.Equal(1, result.SuccessfullyProcessed);
        Assert.Equal(0, result.Failed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ProcessInvoiceBatchAsync_VariousConcurrency_ProcessesCorrectly(int maxParallelism)
    {
        // Arrange
        var invoiceIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var options = new BatchProcessingOptions
        {
            MaxDegreeOfParallelism = maxParallelism
        };

        foreach (var id in invoiceIds)
        {
            _mockUnitOfWork.Setup(x => x.Invoices.GetWithDetailsAsync(id))
                .ReturnsAsync(new Invoice { Id = id, InvoiceNumber = $"INV-{id}" });
        }

        // Act
        var result = await _service.ProcessInvoiceBatchAsync(
            invoiceIds,
            "cert-thumbprint",
            options);

        // Assert
        Assert.Equal(10, result.TotalDocuments);
    }
}
