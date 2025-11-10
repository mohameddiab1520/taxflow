using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;

namespace TaxFlow.Infrastructure.Services.ETA;

/// <summary>
/// ETA submission service implementation
/// </summary>
public class EtaSubmissionService : IEtaSubmissionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IEtaAuthenticationService _authService;

    private string EtaApiUrl => _configuration["ETA:ApiUrl"] ?? "https://api.invoicing.eta.gov.eg/api/v1";

    public EtaSubmissionService(
        HttpClient httpClient,
        IConfiguration configuration,
        IEtaAuthenticationService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _authService = authService;
    }

    public async Task<EtaSubmissionResult> SubmitInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get authentication token
            var token = await _authService.GetAccessTokenAsync(cancellationToken);

            // Serialize invoice to ETA JSON format
            var jsonDocument = SerializeInvoice(invoice);

            // Create submission request
            var request = new HttpRequestMessage(HttpMethod.Post, $"{EtaApiUrl}/documents")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { documents = new[] { jsonDocument } }),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Submit to ETA
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("ETA submission failed for invoice {InvoiceNumber}: {StatusCode} - {Response}",
                    invoice.InvoiceNumber, response.StatusCode, responseContent);

                return new EtaSubmissionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ETA API returned {response.StatusCode}",
                    ValidationErrors = ExtractErrors(responseContent)
                };
            }

            // Parse successful response
            var submissionResponse = JsonSerializer.Deserialize<EtaSubmissionResponse>(responseContent);

            if (submissionResponse?.AcceptedDocuments?.Any() == true)
            {
                var acceptedDoc = submissionResponse.AcceptedDocuments.First();

                Log.Information("Successfully submitted invoice {InvoiceNumber} to ETA. Long ID: {LongId}",
                    invoice.InvoiceNumber, acceptedDoc.LongId);

                return new EtaSubmissionResult
                {
                    IsSuccess = true,
                    SubmissionId = Guid.Parse(acceptedDoc.Uuid),
                    LongId = acceptedDoc.LongId,
                    InternalId = acceptedDoc.InternalId
                };
            }
            else if (submissionResponse?.RejectedDocuments?.Any() == true)
            {
                var rejectedDoc = submissionResponse.RejectedDocuments.First();

                Log.Warning("Invoice {InvoiceNumber} was rejected by ETA: {Errors}",
                    invoice.InvoiceNumber, string.Join(", ", rejectedDoc.Error?.Details ?? new List<string>()));

                return new EtaSubmissionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Document rejected by ETA",
                    ValidationErrors = rejectedDoc.Error?.Details ?? new List<string>()
                };
            }

            return new EtaSubmissionResult
            {
                IsSuccess = false,
                ErrorMessage = "Unexpected response format from ETA"
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error submitting invoice {InvoiceNumber} to ETA", invoice.InvoiceNumber);

            return new EtaSubmissionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<EtaSubmissionResult>> SubmitInvoiceBatchAsync(
        List<Invoice> invoices,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EtaSubmissionResult>();

        // ETA supports batch submission of up to 100 documents
        foreach (var batch in invoices.Chunk(100))
        {
            foreach (var invoice in batch)
            {
                var result = await SubmitInvoiceAsync(invoice, cancellationToken);
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<EtaSubmissionResult> SubmitReceiptAsync(
        Receipt receipt,
        CancellationToken cancellationToken = default)
    {
        // Similar implementation for receipts (B2C)
        // Using Integration Toolkit endpoint instead
        try
        {
            var token = await _authService.GetAccessTokenAsync(cancellationToken);
            var jsonDocument = SerializeReceipt(receipt);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{EtaApiUrl}/receipts")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { receipts = new[] { jsonDocument } }),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new EtaSubmissionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ETA API returned {response.StatusCode}",
                    ValidationErrors = ExtractErrors(responseContent)
                };
            }

            Log.Information("Successfully submitted receipt {ReceiptNumber} to ETA", receipt.ReceiptNumber);

            return new EtaSubmissionResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error submitting receipt {ReceiptNumber} to ETA", receipt.ReceiptNumber);
            return new EtaSubmissionResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<List<EtaSubmissionResult>> SubmitReceiptBatchAsync(
        List<Receipt> receipts,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EtaSubmissionResult>();

        foreach (var receipt in receipts)
        {
            var result = await SubmitReceiptAsync(receipt, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<EtaSubmissionStatus> GetSubmissionStatusAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        // Implementation to check submission status from ETA
        return new EtaSubmissionStatus { Status = "Pending" };
    }

    public async Task<bool> CancelDocumentAsync(
        string etaLongId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        // Implementation to cancel a document in ETA
        return false;
    }

    private object SerializeInvoice(Invoice invoice)
    {
        // Serialize to ETA JSON format according to Egyptian Tax Authority specification
        return new
        {
            issuer = new
            {
                name = invoice.Issuer?.NameEn,
                id = invoice.Issuer?.TaxRegistrationNumber,
                type = invoice.Issuer?.CustomerType,
                address = invoice.Issuer?.Address
            },
            receiver = new
            {
                name = invoice.Customer?.NameEn,
                id = invoice.Customer?.TaxRegistrationNumber,
                type = invoice.Customer?.CustomerType,
                address = invoice.Customer?.Address
            },
            documentType = invoice.DocumentType.ToString(),
            documentTypeVersion = invoice.DocumentTypeVersion,
            dateTimeIssued = invoice.DateTimeIssued.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            taxpayerActivityCode = "4620",
            internalID = invoice.InvoiceNumber,
            invoiceLines = invoice.Lines.Select(line => new
            {
                description = line.DescriptionEn,
                itemType = "GS1",
                itemCode = line.ItemCode,
                unitType = line.UnitType,
                quantity = line.Quantity,
                salesTotal = line.Quantity * line.UnitPrice,
                total = line.TotalAmount,
                valueDifference = line.Discount,
                totalTaxableFees = 0,
                netTotal = line.NetAmount,
                itemsDiscount = line.Discount,
                unitValue = new { currencySold = "EGP", amountEGP = line.UnitPrice },
                discount = new { rate = 0, amount = line.Discount },
                taxableItems = line.TaxItems.Select(tax => new
                {
                    taxType = tax.TaxType.ToString(),
                    amount = tax.TaxValue,
                    subType = tax.SubType,
                    rate = tax.Rate
                })
            }),
            totalSalesAmount = invoice.TotalSalesAmount,
            totalDiscountAmount = invoice.TotalDiscountAmount,
            netAmount = invoice.NetAmount,
            taxTotals = invoice.TaxTotals.Select(tax => new
            {
                taxType = tax.TaxType.ToString(),
                amount = tax.TaxValue
            }),
            totalAmount = invoice.TotalAmount,
            extraDiscountAmount = invoice.ExtraDiscountAmount,
            totalItemsDiscountAmount = invoice.TotalDiscountAmount
        };
    }

    private object SerializeReceipt(Receipt receipt)
    {
        // Serialize receipt to ETA format (similar to invoice but for B2C)
        return new
        {
            header = new
            {
                dateTimeIssued = receipt.DateTimeIssued.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                receiptNumber = receipt.ReceiptNumber,
                uuid = Guid.NewGuid().ToString(),
                previousUUID = "",
                referenceOldUUID = "",
                currency = "EGP",
                exchangeRate = 0,
                sOrderNameCode = ""
            },
            documentType = "R",
            seller = new { },
            buyer = new { },
            itemData = receipt.Lines.Select(line => new
            {
                internalCode = line.ItemCode,
                description = line.DescriptionEn,
                itemType = "GS1",
                itemCode = line.ItemCode,
                unitType = line.UnitType,
                quantity = line.Quantity,
                unitPrice = line.UnitPrice,
                netSale = line.NetAmount,
                totalSale = line.TotalAmount,
                total = line.TotalAmount
            }),
            totalSales = receipt.TotalSalesAmount,
            totalCommercialDiscount = receipt.TotalDiscountAmount,
            netAmount = receipt.NetAmount,
            feesAmount = 0,
            totalAmount = receipt.TotalAmount,
            taxTotals = receipt.TaxTotals.Select(tax => new
            {
                taxType = tax.TaxType.ToString(),
                amount = tax.TaxValue
            }),
            paymentMethod = receipt.PaymentMethod,
            adjustment = 0
        };
    }

    private List<string> ExtractErrors(string responseContent)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<EtaErrorResponse>(responseContent);
            return errorResponse?.Error?.Details ?? new List<string> { responseContent };
        }
        catch
        {
            return new List<string> { responseContent };
        }
    }
}

// ETA API Response Models
internal class EtaSubmissionResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("acceptedDocuments")]
    public List<AcceptedDocument>? AcceptedDocuments { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("rejectedDocuments")]
    public List<RejectedDocument>? RejectedDocuments { get; set; }
}

internal class AcceptedDocument
{
    [System.Text.Json.Serialization.JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("longId")]
    public string LongId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("internalId")]
    public string InternalId { get; set; } = string.Empty;
}

internal class RejectedDocument
{
    [System.Text.Json.Serialization.JsonPropertyName("internalId")]
    public string InternalId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public EtaError? Error { get; set; }
}

internal class EtaErrorResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public EtaError? Error { get; set; }
}

internal class EtaError
{
    [System.Text.Json.Serialization.JsonPropertyName("details")]
    public List<string>? Details { get; set; }
}
