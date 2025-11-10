using FluentValidation;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Enums;

namespace TaxFlow.Application.Validators;

/// <summary>
/// Validator for Invoice entity according to ETA standards
/// </summary>
public class InvoiceValidator : AbstractValidator<Invoice>
{
    public InvoiceValidator()
    {
        // Invoice number is required
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("Invoice number is required")
            .MaximumLength(50).WithMessage("Invoice number must not exceed 50 characters");

        // Date must be valid
        RuleFor(x => x.DateTimeIssued)
            .NotEmpty().WithMessage("Invoice date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Invoice date cannot be in the future");

        // Document type version is required
        RuleFor(x => x.DocumentTypeVersion)
            .NotEmpty().WithMessage("Document type version is required")
            .Must(v => v == "1.0" || v == "0.9")
            .WithMessage("Document type version must be 1.0 or 0.9");

        // Customer is required
        RuleFor(x => x.Customer)
            .NotNull().WithMessage("Customer information is required")
            .SetValidator(new CustomerValidator()!);

        // Must have at least one line
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Invoice must have at least one line item");

        // Validate each line
        RuleForEach(x => x.Lines)
            .SetValidator(new InvoiceLineValidator());

        // Total amount must be positive
        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than zero");

        // Net amount must be positive
        RuleFor(x => x.NetAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Net amount cannot be negative");

        // Extra discount cannot exceed net amount
        RuleFor(x => x.ExtraDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Extra discount cannot be negative")
            .LessThanOrEqualTo(x => x.TotalSalesAmount)
            .WithMessage("Extra discount cannot exceed total sales amount");

        // Validate totals match
        RuleFor(x => x)
            .Must(HaveValidTotals)
            .WithMessage("Invoice totals do not match calculated amounts");
    }

    private bool HaveValidTotals(Invoice invoice)
    {
        if (invoice.Lines == null || !invoice.Lines.Any())
            return true;

        var calculatedNet = invoice.Lines.Sum(l => l.NetAmount) - invoice.ExtraDiscountAmount;
        var calculatedTax = invoice.Lines.Sum(l => l.TotalTaxAmount);
        var calculatedTotal = calculatedNet + calculatedTax;

        // Allow 1 cent rounding difference
        return Math.Abs(calculatedTotal - invoice.TotalAmount) <= 0.01m;
    }
}

/// <summary>
/// Validator for InvoiceLine entity
/// </summary>
public class InvoiceLineValidator : AbstractValidator<InvoiceLine>
{
    public InvoiceLineValidator()
    {
        // Description is required
        RuleFor(x => x.DescriptionAr)
            .NotEmpty().WithMessage("Arabic description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.DescriptionEn)
            .NotEmpty().WithMessage("English description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        // Quantity must be positive
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        // Unit price must be non-negative
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");

        // Unit type is required
        RuleFor(x => x.UnitType)
            .NotEmpty().WithMessage("Unit type is required")
            .MaximumLength(10).WithMessage("Unit type must not exceed 10 characters");

        // Discount cannot be negative
        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative");

        // Discount cannot exceed gross amount
        RuleFor(x => x)
            .Must(x => x.Discount <= (x.Quantity * x.UnitPrice))
            .WithMessage("Discount cannot exceed line total");

        // Must have at least one tax item
        RuleFor(x => x.TaxItems)
            .NotEmpty().WithMessage("Line must have at least one tax item");
    }
}

/// <summary>
/// Validator for Customer entity
/// </summary>
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        // Name is required
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        // Customer type is required
        RuleFor(x => x.CustomerType)
            .NotEmpty().WithMessage("Customer type is required")
            .Must(t => t == "B" || t == "P" || t == "F")
            .WithMessage("Customer type must be B (Business), P (Person), or F (Foreign)");

        // For business customers, tax registration is required
        When(x => x.CustomerType == "B", () =>
        {
            RuleFor(x => x.TaxRegistrationNumber)
                .NotEmpty().WithMessage("Tax registration number is required for business customers")
                .Matches(@"^\d{9}$").WithMessage("Tax registration number must be 9 digits");
        });

        // Address is required
        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required")
            .SetValidator(new AddressValidator()!);

        // Email validation if provided
        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email address");
        });
    }
}

/// <summary>
/// Validator for Address value object
/// </summary>
public class AddressValidator : AbstractValidator<TaxFlow.Core.ValueObjects.Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .Length(2).WithMessage("Country code must be 2 characters");

        RuleFor(x => x.Governate)
            .NotEmpty().WithMessage("Governate is required")
            .MaximumLength(100).WithMessage("Governate cannot exceed 100 characters");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required")
            .MaximumLength(200).WithMessage("Street cannot exceed 200 characters");

        RuleFor(x => x.BuildingNumber)
            .NotEmpty().WithMessage("Building number is required")
            .MaximumLength(50).WithMessage("Building number cannot exceed 50 characters");
    }
}

/// <summary>
/// Validator for Receipt entity
/// </summary>
public class ReceiptValidator : AbstractValidator<Receipt>
{
    public ReceiptValidator()
    {
        RuleFor(x => x.ReceiptNumber)
            .NotEmpty().WithMessage("Receipt number is required")
            .MaximumLength(50).WithMessage("Receipt number must not exceed 50 characters");

        RuleFor(x => x.DateTimeIssued)
            .NotEmpty().WithMessage("Receipt date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Receipt date cannot be in the future");

        RuleFor(x => x.TerminalId)
            .NotEmpty().WithMessage("Terminal ID is required")
            .MaximumLength(50).WithMessage("Terminal ID must not exceed 50 characters");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Receipt must have at least one line item");

        RuleForEach(x => x.Lines)
            .SetValidator(new ReceiptLineValidator());

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than zero");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required");

        // Cash payment validations
        When(x => x.PaymentMethod == "Cash", () =>
        {
            RuleFor(x => x.AmountTendered)
                .NotNull().WithMessage("Amount tendered is required for cash payments")
                .GreaterThanOrEqualTo(x => x.TotalAmount)
                .WithMessage("Amount tendered must be greater than or equal to total amount");
        });
    }
}

/// <summary>
/// Validator for ReceiptLine entity
/// </summary>
public class ReceiptLineValidator : AbstractValidator<ReceiptLine>
{
    public ReceiptLineValidator()
    {
        RuleFor(x => x.DescriptionAr)
            .NotEmpty().WithMessage("Arabic description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.DescriptionEn)
            .NotEmpty().WithMessage("English description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");

        RuleFor(x => x.UnitType)
            .NotEmpty().WithMessage("Unit type is required");

        RuleFor(x => x.TaxItems)
            .NotEmpty().WithMessage("Line must have at least one tax item");
    }
}
