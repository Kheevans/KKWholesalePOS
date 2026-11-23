using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KKWholesalePOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditSaleDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "CreditSales",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                "UPDATE CreditSales " +
                "SET DueDate = datetime(IssuedAt, '+14 days') " +
                "WHERE DueDate = '0001-01-01 00:00:00'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "CreditSales");
        }
    }
}
