using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDailyReportsDeptEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "DailyReports");

            migrationBuilder.AlterColumn<int>(
                name: "Department",
                table: "DailyReports",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedById",
                table: "DailyReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_AssignedById",
                table: "DailyReports",
                column: "AssignedById");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReports_Users_AssignedById",
                table: "DailyReports",
                column: "AssignedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReports_Users_AssignedById",
                table: "DailyReports");

            migrationBuilder.DropIndex(
                name: "IX_DailyReports_AssignedById",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "DailyReports");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "DailyReports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "DailyReports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
