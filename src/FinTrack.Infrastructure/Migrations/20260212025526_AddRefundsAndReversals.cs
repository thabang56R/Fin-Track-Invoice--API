using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundsAndReversals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversedPaymentId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReversedPaymentId",
                table: "Payments");

            
        }
    }
}
