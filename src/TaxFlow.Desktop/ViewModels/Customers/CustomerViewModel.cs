using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Core.ValueObjects;
using TaxFlow.Application.Validators;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Desktop.ViewModels.Customers;

/// <summary>
/// View model for creating/editing customers
/// </summary>
public partial class CustomerViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerViewModel> _logger;

    [ObservableProperty]
    private Guid? _customerId;

    [ObservableProperty]
    private string _nameAr = string.Empty;

    [ObservableProperty]
    private string _nameEn = string.Empty;

    [ObservableProperty]
    private string _taxRegistrationNumber = string.Empty;

    [ObservableProperty]
    private string _commercialRegistrationNumber = string.Empty;

    [ObservableProperty]
    private string _nationalId = string.Empty;

    [ObservableProperty]
    private string _customerType = "B"; // B = Business, P = Person, F = Foreign

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private bool _isTaxExempt = false;

    [ObservableProperty]
    private string _notes = string.Empty;

    // Address fields
    [ObservableProperty]
    private string _country = "EG";

    [ObservableProperty]
    private string _governate = string.Empty;

    [ObservableProperty]
    private string _regionCity = string.Empty;

    [ObservableProperty]
    private string _street = string.Empty;

    [ObservableProperty]
    private string _buildingNumber = string.Empty;

    [ObservableProperty]
    private string _postalCode = string.Empty;

    [ObservableProperty]
    private string _floor = string.Empty;

    [ObservableProperty]
    private string _room = string.Empty;

    [ObservableProperty]
    private string _landmark = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private bool _isNewCustomer = true;

    public List<string> CustomerTypes { get; } = new() { "B", "P", "F" };
    public List<string> EgyptianGovernorates { get; } = new()
    {
        "Cairo", "Giza", "Alexandria", "Aswan", "Asyut", "Beheira",
        "Beni Suef", "Dakahlia", "Damietta", "Faiyum", "Gharbia",
        "Ismailia", "Kafr El Sheikh", "Luxor", "Matruh", "Minya",
        "Monufia", "New Valley", "North Sinai", "Port Said",
        "Qalyubia", "Qena", "Red Sea", "Sharqia", "Sohag",
        "South Sinai", "Suez"
    };

    public CustomerViewModel(
        IUnitOfWork unitOfWork,
        ILogger<CustomerViewModel> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    public async Task InitializeAsync(Guid? customerId = null)
    {
        await ExecuteAsync(async () =>
        {
            if (customerId.HasValue)
            {
                await LoadCustomerAsync(customerId.Value);
                IsNewCustomer = false;
            }
            else
            {
                IsNewCustomer = true;
            }
        }, "Loading customer...");
    }

    /// <summary>
    /// Loads an existing customer
    /// </summary>
    private async Task LoadCustomerAsync(Guid id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
        {
            SetError("Customer not found");
            return;
        }

        CustomerId = customer.Id;
        NameAr = customer.NameAr;
        NameEn = customer.NameEn;
        TaxRegistrationNumber = customer.TaxRegistrationNumber ?? string.Empty;
        CommercialRegistrationNumber = customer.CommercialRegistrationNumber ?? string.Empty;
        NationalId = customer.NationalId ?? string.Empty;
        CustomerType = customer.CustomerType;
        Email = customer.Email ?? string.Empty;
        Phone = customer.Phone ?? string.Empty;
        IsTaxExempt = customer.IsTaxExempt;
        Notes = customer.Notes ?? string.Empty;

        // Load address
        if (customer.Address != null)
        {
            Country = customer.Address.Country;
            Governate = customer.Address.Governate;
            RegionCity = customer.Address.RegionCity ?? string.Empty;
            Street = customer.Address.Street;
            BuildingNumber = customer.Address.BuildingNumber;
            PostalCode = customer.Address.PostalCode ?? string.Empty;
            Floor = customer.Address.Floor ?? string.Empty;
            Room = customer.Address.Room ?? string.Empty;
            Landmark = customer.Address.Landmark ?? string.Empty;
        }
    }

    /// <summary>
    /// Validates the customer
    /// </summary>
    [RelayCommand]
    private async Task ValidateAsync()
    {
        await ExecuteAsync(async () =>
        {
            var customer = ToEntity();
            var validator = new CustomerValidator();
            var result = await validator.ValidateAsync(customer);

            if (result.IsValid)
            {
                IsValid = true;
                ValidationMessage = "✓ Customer data is valid";
            }
            else
            {
                IsValid = false;
                var errors = result.Errors.Select(e => $"• {e.ErrorMessage}");
                ValidationMessage = "✗ Validation errors:\n" + string.Join("\n", errors);
            }
        }, "Validating customer...");
    }

    /// <summary>
    /// Saves the customer
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Validate first
            await ValidateAsync();

            if (!IsValid)
            {
                SetError("Cannot save invalid customer. Please fix validation errors first.");
                return;
            }

            var customer = ToEntity();

            if (IsNewCustomer)
            {
                await _unitOfWork.Customers.AddAsync(customer);
                CustomerId = customer.Id;
            }
            else
            {
                await _unitOfWork.Customers.UpdateAsync(customer);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Customer {Name} saved successfully", NameEn);
            IsNewCustomer = false;

        }, "Saving customer...");
    }

    /// <summary>
    /// Converts view model to entity
    /// </summary>
    private Customer ToEntity()
    {
        return new Customer
        {
            Id = CustomerId ?? Guid.NewGuid(),
            NameAr = NameAr,
            NameEn = NameEn,
            TaxRegistrationNumber = string.IsNullOrWhiteSpace(TaxRegistrationNumber) ? null : TaxRegistrationNumber,
            CommercialRegistrationNumber = string.IsNullOrWhiteSpace(CommercialRegistrationNumber) ? null : CommercialRegistrationNumber,
            NationalId = string.IsNullOrWhiteSpace(NationalId) ? null : NationalId,
            CustomerType = CustomerType,
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email,
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone,
            IsTaxExempt = IsTaxExempt,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            Address = new Address
            {
                Country = Country,
                Governate = Governate,
                RegionCity = string.IsNullOrWhiteSpace(RegionCity) ? null : RegionCity,
                Street = Street,
                BuildingNumber = BuildingNumber,
                PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode,
                Floor = string.IsNullOrWhiteSpace(Floor) ? null : Floor,
                Room = string.IsNullOrWhiteSpace(Room) ? null : Room,
                Landmark = string.IsNullOrWhiteSpace(Landmark) ? null : Landmark
            }
        };
    }
}
