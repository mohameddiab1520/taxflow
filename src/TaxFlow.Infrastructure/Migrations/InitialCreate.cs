using Microsoft.EntityFrameworkCore.Migrations;

namespace TaxFlow.Infrastructure.Migrations;

/// <summary>
/// Initial database migration
/// Creates all tables for TaxFlow system
/// </summary>
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Customers table
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                NameAr = table.Column<string>(maxLength: 200, nullable: false),
                NameEn = table.Column<string>(maxLength: 200, nullable: false),
                TaxRegistrationNumber = table.Column<string>(maxLength: 50, nullable: false),
                CustomerType = table.Column<string>(maxLength: 1, nullable: false),
                Address_Country = table.Column<string>(maxLength: 2, nullable: true),
                Address_Governate = table.Column<string>(maxLength: 100, nullable: true),
                Address_RegionCity = table.Column<string>(maxLength: 100, nullable: true),
                Address_Street = table.Column<string>(maxLength: 200, nullable: true),
                Address_BuildingNumber = table.Column<string>(maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        // Create Invoices table
        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                InvoiceNumber = table.Column<string>(maxLength: 50, nullable: false),
                DocumentType = table.Column<int>(nullable: false),
                DocumentTypeVersion = table.Column<string>(maxLength: 10, nullable: false),
                DateTimeIssued = table.Column<DateTime>(nullable: false),
                TaxpayerActivityCode = table.Column<string>(maxLength: 20, nullable: true),
                CustomerId = table.Column<Guid>(nullable: false),
                TotalSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalTaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<int>(nullable: false),
                EtaUuid = table.Column<string>(maxLength: 100, nullable: true),
                EtaResponse = table.Column<string>(nullable: true),
                Signature = table.Column<string>(nullable: true),
                SubmittedAt = table.Column<DateTime>(nullable: true),
                Notes = table.Column<string>(maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_Invoices_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create InvoiceLines table
        migrationBuilder.CreateTable(
            name: "InvoiceLines",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                InvoiceId = table.Column<Guid>(nullable: false),
                DescriptionAr = table.Column<string>(maxLength: 500, nullable: false),
                DescriptionEn = table.Column<string>(maxLength: 500, nullable: false),
                ItemCode = table.Column<string>(maxLength: 50, nullable: true),
                UnitType = table.Column<string>(maxLength: 10, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_InvoiceLines_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create Receipts table
        migrationBuilder.CreateTable(
            name: "Receipts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                ReceiptNumber = table.Column<string>(maxLength: 50, nullable: false),
                DateTimeIssued = table.Column<DateTime>(nullable: false),
                CustomerId = table.Column<Guid>(nullable: true),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PaymentMethod = table.Column<int>(nullable: false),
                Status = table.Column<int>(nullable: false),
                EtaUuid = table.Column<string>(maxLength: 100, nullable: true),
                SubmittedAt = table.Column<DateTime>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Receipts", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Customers_TaxRegistrationNumber",
            table: "Customers",
            column: "TaxRegistrationNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_CustomerId",
            table: "Invoices",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_InvoiceNumber",
            table: "Invoices",
            column: "InvoiceNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_Status",
            table: "Invoices",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceLines_InvoiceId",
            table: "InvoiceLines",
            column: "InvoiceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InvoiceLines");
        migrationBuilder.DropTable(name: "Receipts");
        migrationBuilder.DropTable(name: "Invoices");
        migrationBuilder.DropTable(name: "Customers");
    }
}
