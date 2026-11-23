using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KKWholesalePOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessName = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessAddress = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiptHeader = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiptFooter = table.Column<string>(type: "TEXT", nullable: false),
                    ShowBusinessContactDetails = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: false),
                    DateFormat = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultMinimumStockLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSettings");
        }
    }
}
