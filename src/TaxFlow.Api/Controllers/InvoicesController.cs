using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;
using TaxFlow.Core.Interfaces;
using TaxFlow.Application.Services;

namespace TaxFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TaxCalculationService _taxCalculationService;
    private readonly IEtaSubmissionService _etaSubmissionService;
    private readonly IDigitalSignatureService _signatureService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IUnitOfWork unitOfWork,
        TaxCalculationService taxCalculationService,
        IEtaSubmissionService etaSubmissionService,
        IDigitalSignatureService signatureService,
        ILogger<InvoicesController> logger)
    {
        _unitOfWork = unitOfWork;
        _taxCalculationService = taxCalculationService;
        _etaSubmissionService = etaSubmissionService;
        _signatureService = signatureService;
        _logger = logger;
    }

    /// <summary>
    /// Get all invoices with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices(
        [FromQuery] DocumentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = await _unitOfWork.Invoices.GetAllAsync();

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(i => i.DateTimeIssued >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.DateTimeIssued <= toDate.Value);

            var total = query.Count();
            var invoices = query
                .OrderByDescending(i => i.DateTimeIssued)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get invoice by ID with full details
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Invoice>> GetInvoice(Guid id)
    {
        try
        {
            var invoice = await _unitOfWork.Invoices.GetWithDetailsAsync(id);

            if (invoice == null)
                return NotFound(new { error = $"Invoice {id} not found" });

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] Invoice invoice)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            invoice.Id = Guid.NewGuid();
            invoice.Status = DocumentStatus.Draft;
            invoice.DateTimeIssued = DateTime.UtcNow;

            // Calculate taxes
            await _taxCalculationService.CalculateTaxesAsync(invoice);

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} created successfully", invoice.InvoiceNumber);

            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return StatusCode(500, new { error = "Failed to create invoice" });
        }
    }

    /// <summary>
    /// Update an existing invoice
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Invoice>> UpdateInvoice(Guid id, [FromBody] Invoice invoice)
    {
        try
        {
            var existing = await _unitOfWork.Invoices.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { error = $"Invoice {id} not found" });

            if (existing.Status != DocumentStatus.Draft)
                return BadRequest(new { error = "Only draft invoices can be updated" });

            // Update properties
            existing.InvoiceNumber = invoice.InvoiceNumber;
            existing.CustomerId = invoice.CustomerId;
            existing.Lines = invoice.Lines;
            existing.Notes = invoice.Notes;

            // Recalculate taxes
            await _taxCalculationService.CalculateTaxesAsync(existing);

            await _unitOfWork.Invoices.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} updated successfully", existing.InvoiceNumber);

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
            return StatusCode(500, new { error = "Failed to update invoice" });
        }
    }

    /// <summary>
    /// Delete an invoice
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteInvoice(Guid id)
    {
        try
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
            if (invoice == null)
                return NotFound(new { error = $"Invoice {id} not found" });

            if (invoice.Status != DocumentStatus.Draft)
                return BadRequest(new { error = "Only draft invoices can be deleted" });

            await _unitOfWork.Invoices.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} deleted successfully", invoice.InvoiceNumber);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
            return StatusCode(500, new { error = "Failed to delete invoice" });
        }
    }

    /// <summary>
    /// Submit invoice to ETA
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> SubmitInvoice(Guid id)
    {
        try
        {
            var invoice = await _unitOfWork.Invoices.GetWithDetailsAsync(id);
            if (invoice == null)
                return NotFound(new { error = $"Invoice {id} not found" });

            if (invoice.Status != DocumentStatus.Draft && invoice.Status != DocumentStatus.Valid)
                return BadRequest(new { error = "Invoice must be in draft or valid status" });

            // Sign invoice
            var signature = await _signatureService.SignDocumentAsync(
                System.Text.Json.JsonSerializer.Serialize(invoice));
            invoice.Signature = signature;

            // Submit to ETA
            var result = await _etaSubmissionService.SubmitInvoiceAsync(invoice);

            if (result.IsSuccess)
            {
                invoice.Status = DocumentStatus.Submitted;
                invoice.SubmittedAt = DateTime.UtcNow;
                invoice.EtaUuid = result.Uuid;
                invoice.EtaResponse = result.Message;

                await _unitOfWork.Invoices.UpdateAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Invoice {InvoiceNumber} submitted successfully to ETA", invoice.InvoiceNumber);

                return Ok(new
                {
                    success = true,
                    message = "Invoice submitted successfully",
                    uuid = result.Uuid
                });
            }
            else
            {
                invoice.Status = DocumentStatus.Rejected;
                invoice.EtaResponse = result.Message;

                await _unitOfWork.Invoices.UpdateAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning("Invoice {InvoiceNumber} submission failed: {Error}", invoice.InvoiceNumber, result.Message);

                return BadRequest(new
                {
                    success = false,
                    error = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting invoice {InvoiceId}", id);
            return StatusCode(500, new { error = "Failed to submit invoice" });
        }
    }

    /// <summary>
    /// Get invoice statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        try
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();

            var stats = new
            {
                Total = invoices.Count(),
                Draft = invoices.Count(i => i.Status == DocumentStatus.Draft),
                Valid = invoices.Count(i => i.Status == DocumentStatus.Valid),
                Submitted = invoices.Count(i => i.Status == DocumentStatus.Submitted),
                Rejected = invoices.Count(i => i.Status == DocumentStatus.Rejected),
                TotalAmount = invoices.Sum(i => i.TotalAmount),
                AverageAmount = invoices.Any() ? invoices.Average(i => i.TotalAmount) : 0,
                ThisMonth = invoices.Count(i => i.DateTimeIssued.Month == DateTime.UtcNow.Month
                                              && i.DateTimeIssued.Year == DateTime.UtcNow.Year)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
