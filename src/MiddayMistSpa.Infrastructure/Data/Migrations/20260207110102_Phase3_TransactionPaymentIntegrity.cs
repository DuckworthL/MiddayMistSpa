using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiddayMistSpa.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_TransactionPaymentIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountTendered",
                table: "Transactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPointsEarned",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Transactions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountTendered",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ChangeAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsEarned",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Transactions");
        }
    }
}
