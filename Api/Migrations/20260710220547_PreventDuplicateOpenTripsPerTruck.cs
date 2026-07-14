using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateOpenTripsPerTruck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Trips_TruckId_OpenStatus",
                table: "Trips",
                column: "TruckId",
                unique: true,
                filter: "\"Status\" IN (0, 2, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Trips_TruckId_OpenStatus",
                table: "Trips");
        }
    }
}
