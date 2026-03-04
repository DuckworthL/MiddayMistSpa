using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiddayMistSpa.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodes",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecretKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorConfirmedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecoveryCodes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotpSecretKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorConfirmedAt",
                table: "Users");
        }
    }
}
