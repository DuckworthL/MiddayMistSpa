using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiddayMistSpa.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_AccountingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReversalOfEntryId",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "JournalEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedBy",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalBalance",
                table: "ChartOfAccounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Debit");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReversalOfEntryId",
                table: "JournalEntries",
                column: "ReversalOfEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_VoidedBy",
                table: "JournalEntries",
                column: "VoidedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_JournalEntries_ReversalOfEntryId",
                table: "JournalEntries",
                column: "ReversalOfEntryId",
                principalTable: "JournalEntries",
                principalColumn: "JournalEntryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_VoidedBy",
                table: "JournalEntries",
                column: "VoidedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_JournalEntries_ReversalOfEntryId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Users_VoidedBy",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ReversalOfEntryId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_VoidedBy",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ReversalOfEntryId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "NormalBalance",
                table: "ChartOfAccounts");
        }
    }
}
