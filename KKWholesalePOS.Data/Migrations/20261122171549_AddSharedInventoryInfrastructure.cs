using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KKWholesalePOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedInventoryInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsignmentId",
                table: "StockMovements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsignmentReturnId",
                table: "StockMovements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "StockMovements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStockLevel",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Consignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalGoodsValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    ConsignmentFee = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consignments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentLines_Consignments_ConsignmentId",
                        column: x => x.ConsignmentId,
                        principalTable: "Consignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsignmentLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentPayments_Consignments_ConsignmentId",
                        column: x => x.ConsignmentId,
                        principalTable: "Consignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsignmentPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentReturns_Consignments_ConsignmentId",
                        column: x => x.ConsignmentId,
                        principalTable: "Consignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsignmentReturns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsignmentReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsignmentLineId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityReturned = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityAccepted = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentReturnLines_ConsignmentLines_ConsignmentLineId",
                        column: x => x.ConsignmentLineId,
                        principalTable: "ConsignmentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsignmentReturnLines_ConsignmentReturns_ConsignmentReturnId",
                        column: x => x.ConsignmentReturnId,
                        principalTable: "ConsignmentReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ConsignmentId",
                table: "StockMovements",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ConsignmentReturnId",
                table: "StockMovements",
                column: "ConsignmentReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentLines_ConsignmentId",
                table: "ConsignmentLines",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentLines_ProductId",
                table: "ConsignmentLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentPayments_ConsignmentId",
                table: "ConsignmentPayments",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentPayments_UserId",
                table: "ConsignmentPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentReturnLines_ConsignmentLineId",
                table: "ConsignmentReturnLines",
                column: "ConsignmentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentReturnLines_ConsignmentReturnId",
                table: "ConsignmentReturnLines",
                column: "ConsignmentReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentReturns_ConsignmentId",
                table: "ConsignmentReturns",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentReturns_ReferenceNumber",
                table: "ConsignmentReturns",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentReturns_UserId",
                table: "ConsignmentReturns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_CustomerId",
                table: "Consignments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_ReferenceNumber",
                table: "Consignments",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consignments_UserId",
                table: "Consignments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ConsignmentReturns_ConsignmentReturnId",
                table: "StockMovements",
                column: "ConsignmentReturnId",
                principalTable: "ConsignmentReturns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Consignments_ConsignmentId",
                table: "StockMovements",
                column: "ConsignmentId",
                principalTable: "Consignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ConsignmentReturns_ConsignmentReturnId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Consignments_ConsignmentId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "ConsignmentPayments");

            migrationBuilder.DropTable(
                name: "ConsignmentReturnLines");

            migrationBuilder.DropTable(
                name: "ConsignmentLines");

            migrationBuilder.DropTable(
                name: "ConsignmentReturns");

            migrationBuilder.DropTable(
                name: "Consignments");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ConsignmentId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ConsignmentReturnId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ConsignmentId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ConsignmentReturnId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "MinimumStockLevel",
                table: "Products");
        }
    }
}
