using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPhoneUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Drivers_PhoneNo",
                table: "Drivers",
                column: "PhoneNo",
                unique: true,
                filter: "\"PhoneNo\" IS NOT NULL AND \"PhoneNo\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Drivers_PhoneNo",
                table: "Drivers");
        }
    }
}
