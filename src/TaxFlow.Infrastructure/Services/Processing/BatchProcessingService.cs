using System.Diagnostics;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Processing;

/// <summary>
/// Batch processing service with error recovery and retry logic
/// </summary>
public class BatchProcessingService : IBatchProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDigitalSignatureService _signatureService;
    private readonly IEtaSubmissionService _etaService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BatchProcessingService> _logger;

    private readonly Dictionary<Guid, CancellationTokenSource> _runningBatches = new();

    public BatchProcessingService(
        IUnitOfWork unitOfWork,
        IDigitalSignatureService signatureService,
        IEtaSubmissionService etaService,
        INotificationService notificationService,
        ILogger<BatchProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _signatureService = signatureService;
        _etaService = etaService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<BatchProcessingResult> ProcessInvoiceBatchAsync(
        List<Guid> invoiceIds,
        string certificateThumbprint,
        BatchProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid();
        var result = new BatchProcessingResult
        {
            BatchId = batchId,
            TotalDocuments = invoiceIds.Count,
            StartedAt = DateTime.UtcNow
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningBatches[batchId] = cts;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting batch processing of {Count} invoices with batch ID {BatchId}",
                invoiceIds.Count,
                batchId);

            // Process invoices in parallel with controlled degree of parallelism
            var tasks = new List<Task<BatchItemResult>>();

            foreach (var invoiceId in invoiceIds)
            {
                if (cts.Token.IsCancellationRequested)
                    break;

                tasks.Add(ProcessInvoiceWithRetryAsync(
                    invoiceId,
                    certificateThumbprint,
                    options,
                    cts.Token));

                // Control parallelism
                if (tasks.Count >= options.MaxDegreeOfParallelism)
                {
                    var completed = await Task.WhenAny(tasks);
                    var itemResult = await completed;
                    result.ItemResults.Add(itemResult);
                    tasks.Remove(completed);

                    if (itemResult.IsSuccess)
                        result.SuccessfullyProcessed++;
                    else
                        result.Failed++;
                }
            }

            // Wait for remaining tasks
            var remainingResults = await Task.WhenAll(tasks);
            result.ItemResults.AddRange(remainingResults);

            result.SuccessfullyProcessed += remainingResults.Count(r => r.IsSuccess);
            result.Failed += remainingResults.Count(r => !r.IsSuccess);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Batch {BatchId} completed: {Success}/{Total} successful in {Duration:mm\\:ss}",
                batchId,
                result.SuccessfullyProcessed,
                result.TotalDocuments,
                result.Duration);

            // Send notification
            await _notificationService.SendNotificationAsync(new Notification
            {
                Type = NotificationType.BatchCompleted,
                Title = "Batch Processing Completed",
                Message = $"Processed {result.SuccessfullyProcessed}/{result.TotalDocuments} invoices successfully",
                Severity = result.Failed > 0 ? NotificationSeverity.Warning : NotificationSeverity.Success
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch {BatchId}", batchId);
            result.ErrorMessage = ex.Message;

            await _notificationService.SendNotificationAsync(new Notification
            {
                Type = NotificationType.SystemError,
                Title = "Batch Processing Failed",
                Message = $"Batch processing failed: {ex.Message}",
                Severity = NotificationSeverity.Error
            }, cancellationToken);
        }
        finally
        {
            _runningBatches.Remove(batchId);
            cts.Dispose();
        }

        return result;
    }

    public async Task<BatchProcessingResult> ProcessReceiptBatchAsync(
        List<Guid> receiptIds,
        string certificateThumbprint,
        BatchProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid();
        var result = new BatchProcessingResult
        {
            BatchId = batchId,
            TotalDocuments = receiptIds.Count,
            StartedAt = DateTime.UtcNow
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningBatches[batchId] = cts;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting batch processing of {Count} receipts with batch ID {BatchId}",
                receiptIds.Count,
                batchId);

            var tasks = new List<Task<BatchItemResult>>();

            foreach (var receiptId in receiptIds)
            {
                if (cts.Token.IsCancellationRequested)
                    break;

                tasks.Add(ProcessReceiptWithRetryAsync(
                    receiptId,
                    certificateThumbprint,
                    options,
                    cts.Token));

                if (tasks.Count >= options.MaxDegreeOfParallelism)
                {
                    var completed = await Task.WhenAny(tasks);
                    var itemResult = await completed;
                    result.ItemResults.Add(itemResult);
                    tasks.Remove(completed);

                    if (itemResult.IsSuccess)
                        result.SuccessfullyProcessed++;
                    else
                        result.Failed++;
                }
            }

            var remainingResults = await Task.WhenAll(tasks);
            result.ItemResults.AddRange(remainingResults);

            result.SuccessfullyProcessed += remainingResults.Count(r => r.IsSuccess);
            result.Failed += remainingResults.Count(r => !r.IsSuccess);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Receipt batch {BatchId} completed: {Success}/{Total} successful",
                batchId,
                result.SuccessfullyProcessed,
                result.TotalDocuments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt batch {BatchId}", batchId);
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            _runningBatches.Remove(batchId);
            cts.Dispose();
        }

        return result;
    }

    private async Task<BatchItemResult> ProcessInvoiceWithRetryAsync(
        Guid invoiceId,
        string certificateThumbprint,
        BatchProcessingOptions options,
        CancellationToken cancellationToken)
    {
        var result = new BatchItemResult { DocumentId = invoiceId };
        var attempt = 0;

        while (attempt < options.MaxRetryAttempts)
        {
            attempt++;
            result.RetryAttempts = attempt;

            try
            {
                // Load invoice with all details
                var invoice = await _unitOfWork.Invoices.GetWithDetailsAsync(invoiceId);
                if (invoice == null)
                {
                    result.ErrorMessage = "Invoice not found";
                    return result;
                }

                result.DocumentNumber = invoice.InvoiceNumber;

                // Update status to submitting
                invoice.Status = DocumentStatus.Submitting;
                await _unitOfWork.CommitAsync();

                // Sign the document (if not already signed)
                if (string.IsNullOrEmpty(invoice.Signature))
                {
                    // TODO: Serialize invoice to JSON
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(invoice);
                    var signResult = await _signatureService.SignJsonDocumentAsync(
                        jsonContent,
                        certificateThumbprint,
                        cancellationToken);

                    if (!signResult.IsSuccess)
                    {
                        throw new Exception($"Signing failed: {signResult.ErrorMessage}");
                    }

                    invoice.Signature = signResult.SignatureValue;
                    await _unitOfWork.CommitAsync();
                }

                // Submit to ETA
                var submissionResult = await _etaService.SubmitInvoiceAsync(invoice, cancellationToken);

                if (submissionResult.IsSuccess)
                {
                    invoice.Status = DocumentStatus.Submitted;
                    invoice.EtaLongId = submissionResult.LongId;
                    invoice.SubmittedAt = DateTime.UtcNow;
                    await _unitOfWork.CommitAsync();

                    result.IsSuccess = true;
                    result.EtaLongId = submissionResult.LongId;

                    _logger.LogInformation(
                        "Successfully processed invoice {InvoiceNumber} (Attempt {Attempt})",
                        invoice.InvoiceNumber,
                        attempt);

                    return result;
                }
                else
                {
                    throw new Exception($"ETA submission failed: {submissionResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;

                _logger.LogWarning(
                    ex,
                    "Failed to process invoice {InvoiceId} (Attempt {Attempt}/{Max})",
                    invoiceId,
                    attempt,
                    options.MaxRetryAttempts);

                if (attempt < options.MaxRetryAttempts)
                {
                    // Exponential backoff
                    var delay = options.RetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    // Mark as failed after all retries
                    try
                    {
                        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
                        if (invoice != null)
                        {
                            invoice.Status = DocumentStatus.Rejected;
                            await _unitOfWork.CommitAsync();
                        }
                    }
                    catch { /* Ignore errors when updating status */ }
                }
            }
        }

        return result;
    }

    private async Task<BatchItemResult> ProcessReceiptWithRetryAsync(
        Guid receiptId,
        string certificateThumbprint,
        BatchProcessingOptions options,
        CancellationToken cancellationToken)
    {
        var result = new BatchItemResult { DocumentId = receiptId };
        var attempt = 0;

        while (attempt < options.MaxRetryAttempts)
        {
            attempt++;
            result.RetryAttempts = attempt;

            try
            {
                var receipt = await _unitOfWork.Receipts.GetWithDetailsAsync(receiptId);
                if (receipt == null)
                {
                    result.ErrorMessage = "Receipt not found";
                    return result;
                }

                result.DocumentNumber = receipt.ReceiptNumber;

                receipt.Status = DocumentStatus.Submitting;
                await _unitOfWork.CommitAsync();

                // Sign and submit
                if (string.IsNullOrEmpty(receipt.Signature))
                {
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(receipt);
                    var signResult = await _signatureService.SignJsonDocumentAsync(
                        jsonContent,
                        certificateThumbprint,
                        cancellationToken);

                    if (!signResult.IsSuccess)
                    {
                        throw new Exception($"Signing failed: {signResult.ErrorMessage}");
                    }

                    receipt.Signature = signResult.SignatureValue;
                    await _unitOfWork.CommitAsync();
                }

                var submissionResult = await _etaService.SubmitReceiptAsync(receipt, cancellationToken);

                if (submissionResult.IsSuccess)
                {
                    receipt.Status = DocumentStatus.Submitted;
                    receipt.SubmittedAt = DateTime.UtcNow;
                    await _unitOfWork.CommitAsync();

                    result.IsSuccess = true;
                    return result;
                }
                else
                {
                    throw new Exception($"ETA submission failed: {submissionResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;

                if (attempt < options.MaxRetryAttempts)
                {
                    var delay = options.RetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        return result;
    }

    public async Task<BatchProcessingResult> RetryFailedSubmissionsAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement retry logic for previously failed batch
        throw new NotImplementedException();
    }

    public async Task<BatchProcessingStatus> GetBatchStatusAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement batch status tracking
        return new BatchProcessingStatus
        {
            BatchId = batchId,
            Status = _runningBatches.ContainsKey(batchId) ? "Processing" : "Completed"
        };
    }

    public async Task<bool> CancelBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        if (_runningBatches.TryGetValue(batchId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Batch {BatchId} cancellation requested", batchId);
            return true;
        }

        return false;
    }
}
