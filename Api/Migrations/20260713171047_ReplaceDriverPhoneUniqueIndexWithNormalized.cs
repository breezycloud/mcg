using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDriverPhoneUniqueIndexWithNormalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Drivers_PhoneNo",
                table: "Drivers");

            // EF's fluent API can only index an actual column, not an expression, so this one is
            // hand-written raw SQL rather than migrationBuilder.CreateIndex. Keys on the last 10
            // digits (matching Shared/Helpers/PhoneNumberHelper.NormalizeForComparison) so
            // "8069993037" and "08069993037" collide as the same number at the database level —
            // the exact-text index this replaces let three such pairs coexist undetected.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_Drivers_PhoneNo_Normalized"
                ON "Drivers" (right(regexp_replace("PhoneNo", '\D', '', 'g'), 10))
                WHERE "PhoneNo" IS NOT NULL AND "PhoneNo" <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"UX_Drivers_PhoneNo_Normalized\";");

            migrationBuilder.CreateIndex(
                name: "UX_Drivers_PhoneNo",
                table: "Drivers",
                column: "PhoneNo",
                unique: true,
                filter: "\"PhoneNo\" IS NOT NULL AND \"PhoneNo\" != ''");
        }
    }
}
