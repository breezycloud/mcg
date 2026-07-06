using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorMates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentMotorMateId",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MotorMates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PhoneNo = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorMates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MotorMateHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousMotorMateId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewMotorMateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorMateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotorMateHistories_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MotorMateHistories_MotorMates_NewMotorMateId",
                        column: x => x.NewMotorMateId,
                        principalTable: "MotorMates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MotorMateHistories_MotorMates_PreviousMotorMateId",
                        column: x => x.PreviousMotorMateId,
                        principalTable: "MotorMates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MotorMateHistories_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CurrentMotorMateId",
                table: "Drivers",
                column: "CurrentMotorMateId");

            migrationBuilder.CreateIndex(
                name: "IX_MotorMateHistories_ChangedById",
                table: "MotorMateHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_MotorMateHistories_DriverId",
                table: "MotorMateHistories",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_MotorMateHistories_NewMotorMateId",
                table: "MotorMateHistories",
                column: "NewMotorMateId");

            migrationBuilder.CreateIndex(
                name: "IX_MotorMateHistories_PreviousMotorMateId",
                table: "MotorMateHistories",
                column: "PreviousMotorMateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_MotorMates_CurrentMotorMateId",
                table: "Drivers",
                column: "CurrentMotorMateId",
                principalTable: "MotorMates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_MotorMates_CurrentMotorMateId",
                table: "Drivers");

            migrationBuilder.DropTable(
                name: "MotorMateHistories");

            migrationBuilder.DropTable(
                name: "MotorMates");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CurrentMotorMateId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CurrentMotorMateId",
                table: "Drivers");
        }
    }
}
