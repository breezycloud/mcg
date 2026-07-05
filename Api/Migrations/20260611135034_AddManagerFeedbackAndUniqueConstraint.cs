using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerFeedbackAndUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyReports_Employee_Date",
                table: "DailyReports");

            migrationBuilder.AddColumn<string>(
                name: "ManagerComment",
                table: "DailyReports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "DailyReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "DailyReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_ReviewedById",
                table: "DailyReports",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "UX_DailyReports_Employee_Date",
                table: "DailyReports",
                columns: new[] { "EmployeeId", "ReportDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReports_Users_ReviewedById",
                table: "DailyReports",
                column: "ReviewedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReports_Users_ReviewedById",
                table: "DailyReports");

            migrationBuilder.DropIndex(
                name: "IX_DailyReports_ReviewedById",
                table: "DailyReports");

            migrationBuilder.DropIndex(
                name: "UX_DailyReports_Employee_Date",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "ManagerComment",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "DailyReports");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_Employee_Date",
                table: "DailyReports",
                columns: new[] { "EmployeeId", "ReportDate" });
        }
    }
}
