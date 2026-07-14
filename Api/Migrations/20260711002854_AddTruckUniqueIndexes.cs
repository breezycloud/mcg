using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTruckUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Trucks_LicensePlate",
                table: "Trucks",
                column: "LicensePlate",
                unique: true,
                filter: "\"LicensePlate\" IS NOT NULL AND \"LicensePlate\" != ''");

            migrationBuilder.CreateIndex(
                name: "UX_Trucks_TruckNo",
                table: "Trucks",
                column: "TruckNo",
                unique: true,
                filter: "\"TruckNo\" IS NOT NULL AND \"TruckNo\" != ''");

            migrationBuilder.CreateIndex(
                name: "UX_Trucks_VIN",
                table: "Trucks",
                column: "VIN",
                unique: true,
                filter: "\"VIN\" IS NOT NULL AND \"VIN\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Trucks_LicensePlate",
                table: "Trucks");

            migrationBuilder.DropIndex(
                name: "UX_Trucks_TruckNo",
                table: "Trucks");

            migrationBuilder.DropIndex(
                name: "UX_Trucks_VIN",
                table: "Trucks");
        }
    }
}
