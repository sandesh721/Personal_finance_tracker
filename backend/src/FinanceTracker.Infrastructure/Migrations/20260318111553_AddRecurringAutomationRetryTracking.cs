using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringAutomationRetryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "recurring_transaction_executions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedUtc",
                table: "recurring_transaction_executions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAfterUtc",
                table: "recurring_transaction_executions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_transaction_executions_Status_NextRetryAfterUtc",
                table: "recurring_transaction_executions",
                columns: new[] { "Status", "NextRetryAfterUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recurring_transaction_executions_Status_NextRetryAfterUtc",
                table: "recurring_transaction_executions");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "recurring_transaction_executions");

            migrationBuilder.DropColumn(
                name: "LastAttemptedUtc",
                table: "recurring_transaction_executions");

            migrationBuilder.DropColumn(
                name: "NextRetryAfterUtc",
                table: "recurring_transaction_executions");
        }
    }
}
