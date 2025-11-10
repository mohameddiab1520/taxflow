# Contributing to TaxFlow Enterprise

Thank you for your interest in contributing to TaxFlow Enterprise! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing](#testing)
- [Documentation](#documentation)

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

### Our Standards

- Use welcoming and inclusive language
- Be respectful of differing viewpoints and experiences
- Gracefully accept constructive criticism
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

1. **Fork the repository**
   ```bash
   git clone https://github.com/yourusername/taxflow.git
   cd taxflow
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**

4. **Test your changes**
   ```bash
   dotnet test
   ```

5. **Submit a pull request**

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 (17.8+) or JetBrains Rider
- SQLite (included with .NET)
- PostgreSQL 14+ (optional, for analytics)
- Redis 7+ (optional, for caching)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/taxflow.git
   cd taxflow
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Configure settings**
   - Copy `.env.example` to `.env`
   - Update the values with your configuration

4. **Build the solution**
   ```bash
   dotnet build
   ```

5. **Run the application**
   ```bash
   cd src/TaxFlow.Desktop
   dotnet run
   ```

## Coding Standards

### C# Style Guidelines

- Follow the [.NET coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods focused and under 50 lines when possible
- Use `async/await` for I/O operations
- Always use `var` when the type is obvious

### Project Structure

```
TaxFlow.Enterprise/
├── src/
│   ├── TaxFlow.Core/           # Domain layer (entities, interfaces)
│   ├── TaxFlow.Infrastructure/ # Data access and external services
│   ├── TaxFlow.Application/    # Business logic
│   ├── TaxFlow.Desktop/        # WPF UI
│   └── TaxFlow.Api/            # REST API
├── tests/
│   ├── TaxFlow.Tests.Unit/
│   ├── TaxFlow.Tests.Integration/
│   └── TaxFlow.Tests.Performance/
└── docs/
```

### Clean Architecture Principles

- **Core Layer**: Contains entities, value objects, enums, and interfaces. No dependencies.
- **Infrastructure Layer**: Implements interfaces from Core. Can reference Core only.
- **Application Layer**: Business logic. References Core and Infrastructure.
- **Presentation Layer**: UI and API. References all layers.

### Naming Conventions

- **Interfaces**: Start with `I` (e.g., `IInvoiceRepository`)
- **Classes**: PascalCase (e.g., `InvoiceService`)
- **Methods**: PascalCase (e.g., `GetInvoiceById`)
- **Private fields**: `_camelCase` (e.g., `_dbContext`)
- **Properties**: PascalCase (e.g., `InvoiceNumber`)
- **Local variables**: camelCase (e.g., `invoiceId`)

### Code Examples

**Good:**
```csharp
public async Task<Invoice> GetInvoiceByIdAsync(Guid id)
{
    var invoice = await _repository.GetByIdAsync(id);
    if (invoice == null)
        throw new NotFoundException($"Invoice {id} not found");

    return invoice;
}
```

**Avoid:**
```csharp
// Don't use non-descriptive names
public async Task<Invoice> Get(Guid x)
{
    var i = await _repo.GetByIdAsync(x);
    return i;
}
```

## Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) specification.

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that don't affect code meaning (formatting, etc.)
- **refactor**: Code change that neither fixes a bug nor adds a feature
- **perf**: Performance improvements
- **test**: Adding or updating tests
- **chore**: Changes to build process or auxiliary tools

### Examples

```bash
feat(invoice): add bulk invoice submission
fix(eta): handle timeout errors in submission
docs(readme): update installation instructions
test(invoice): add unit tests for validation
```

## Pull Request Process

1. **Update documentation** if you've changed APIs or added features

2. **Add tests** for new functionality
   - Unit tests for business logic
   - Integration tests for data access
   - Performance tests for critical paths

3. **Ensure all tests pass**
   ```bash
   dotnet test
   ```

4. **Update the CHANGELOG.md** with your changes

5. **Create a Pull Request** with:
   - Clear title describing the change
   - Description of what changed and why
   - Reference to any related issues
   - Screenshots for UI changes

6. **Code Review**
   - Address reviewer feedback
   - Keep commits clean and organized
   - Squash commits if requested

7. **Merge Requirements**
   - At least one approval from maintainers
   - All CI checks passing
   - No merge conflicts
   - Branch up to date with main

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TaxFlow.Tests.Unit

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Writing Tests

- Follow AAA pattern: Arrange, Act, Assert
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Mock external dependencies
- Test edge cases and error conditions

**Example:**
```csharp
[Fact]
public async Task CalculateInvoiceTotals_WithDiscount_ReturnsCorrectTotal()
{
    // Arrange
    var invoice = new Invoice { /* ... */ };
    var service = new TaxCalculationService();

    // Act
    var result = service.CalculateInvoiceTotals(invoice);

    // Assert
    Assert.Equal(expectedTotal, result.TotalAmount);
}
```

## Documentation

### Code Documentation

- Add XML comments to all public APIs
- Document complex algorithms
- Explain "why" not "what" in comments

**Example:**
```csharp
/// <summary>
/// Calculates tax totals for an invoice including VAT and other applicable taxes.
/// </summary>
/// <param name="invoice">The invoice to calculate taxes for</param>
/// <returns>Updated invoice with calculated tax amounts</returns>
/// <exception cref="ValidationException">Thrown when invoice data is invalid</exception>
public async Task<Invoice> CalculateTaxesAsync(Invoice invoice)
{
    // Implementation
}
```

### README Updates

- Keep README.md up to date with new features
- Update installation instructions if dependencies change
- Add examples for new APIs

## Questions?

- **Issues**: Open an issue for bugs or feature requests
- **Discussions**: Use GitHub Discussions for questions
- **Email**: support@taxflow.com

## Recognition

Contributors will be recognized in:
- README.md Contributors section
- Release notes for their contributions

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to TaxFlow Enterprise! 🎉
