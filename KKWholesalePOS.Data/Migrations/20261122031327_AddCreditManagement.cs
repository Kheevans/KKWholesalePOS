using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KKWholesalePOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PurchaseAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditSales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditSales_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreditSaleId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditPayments_CreditSales_CreditSaleId",
                        column: x => x.CreditSaleId,
                        principalTable: "CreditSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditSaleLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreditSaleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditSaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditSaleLines_CreditSales_CreditSaleId",
                        column: x => x.CreditSaleId,
                        principalTable: "CreditSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditSaleLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditPayments_CreditSaleId",
                table: "CreditPayments",
                column: "CreditSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditPayments_UserId",
                table: "CreditPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditSaleLines_CreditSaleId",
                table: "CreditSaleLines",
                column: "CreditSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditSaleLines_ProductId",
                table: "CreditSaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditSales_CustomerId",
                table: "CreditSales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditSales_ReferenceNumber",
                table: "CreditSales",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditSales_UserId",
                table: "CreditSales",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditPayments");

            migrationBuilder.DropTable(
                name: "CreditSaleLines");

            migrationBuilder.DropTable(
                name: "CreditSales");
        }
    }
}
