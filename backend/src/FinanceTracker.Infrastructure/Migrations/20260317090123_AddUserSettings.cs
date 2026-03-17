using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LandingPage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Theme = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BudgetWarningsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GoalRemindersEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RecurringRemindersEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultPaymentMethod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DefaultBudgetAlertThresholdPercent = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_settings_accounts_DefaultAccountId",
                        column: x => x.DefaultAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_settings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_DefaultAccountId",
                table: "user_settings",
                column: "DefaultAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_UserId",
                table: "user_settings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
