using Xunit;
using TaxFlow.Application.Services;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;

namespace TaxFlow.Tests.Unit.Services;

public class TaxCalculationServiceTests
{
    private readonly TaxCalculationService _service;

    public TaxCalculationServiceTests()
    {
        _service = new TaxCalculationService();
    }

    [Fact]
    public void CalculateLineTax_WithVAT_ReturnsCorrectAmount()
    {
        // Arrange
        var line = new InvoiceLine
        {
            Quantity = 10,
            UnitPrice = 100,
            Discount = 0
        };

        // Act
        var result = _service.CalculateLineTax(line, TaxType.ValueAddedTax, 14);

        // Assert
        Assert.Equal(140.00m, result, 2);
    }

    [Fact]
    public void CalculateLineTax_WithDiscount_ReturnsCorrectAmount()
    {
        // Arrange
        var line = new InvoiceLine
        {
            Quantity = 10,
            UnitPrice = 100,
            Discount = 200  // 200 EGP discount
        };

        // Act
        var result = _service.CalculateLineTax(line, TaxType.ValueAddedTax, 14);

        // Assert
        // Net = (10 * 100) - 200 = 800
        // Tax = 800 * 0.14 = 112
        Assert.Equal(112.00m, result, 2);
    }

    [Fact]
    public void CalculateInvoiceTotals_MultipleLines_ReturnsCorrectTotals()
    {
        // Arrange
        var invoice = new Invoice
        {
            Lines = new List<InvoiceLine>
            {
                new() { Quantity = 10, UnitPrice = 100, Discount = 0 },
                new() { Quantity = 5, UnitPrice = 50, Discount = 0 }
            }
        };

        foreach (var line in invoice.Lines)
        {
            line.TaxItems = new List<TaxItem>
            {
                new() { TaxType = TaxType.ValueAddedTax, TaxRate = 14 }
            };
        }

        // Act
        _service.CalculateInvoiceTotals(invoice);

        // Assert
        Assert.Equal(1250.00m, invoice.TotalSalesAmount);  // 1000 + 250
        Assert.Equal(1250.00m, invoice.NetAmount);          // No discount
        Assert.Equal(175.00m, invoice.TotalTaxAmount);      // 140 + 35
        Assert.Equal(1425.00m, invoice.TotalAmount);        // 1250 + 175
    }

    [Fact]
    public void CalculateInvoiceTotals_WithZeroAmount_ReturnsZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            Lines = new List<InvoiceLine>()
        };

        // Act
        _service.CalculateInvoiceTotals(invoice);

        // Assert
        Assert.Equal(0m, invoice.TotalAmount);
    }

    [Theory]
    [InlineData(100, 14, 14)]
    [InlineData(1000, 14, 140)]
    [InlineData(500.50, 14, 70.07)]
    public void CalculateLineTax_VariousAmounts_ReturnsCorrectTax(decimal amount, decimal rate, decimal expectedTax)
    {
        // Arrange
        var line = new InvoiceLine
        {
            Quantity = 1,
            UnitPrice = amount,
            Discount = 0
        };

        // Act
        var result = _service.CalculateLineTax(line, TaxType.ValueAddedTax, rate);

        // Assert
        Assert.Equal(expectedTax, result, 2);
    }
}
