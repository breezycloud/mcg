using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorMatePhoneUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MotorMate.PhoneNo never had ANY uniqueness guard (unlike Drivers, which at least
            // had an exact-text index before it too got replaced by the normalized version — see
            // ReplaceDriverPhoneUniqueIndexWithNormalized). Same expression-index approach: EF's
            // fluent API can only index an actual column, not an expression, so this is raw SQL,
            // keyed on the last 10 digits to match Shared/Helpers/PhoneNumberHelper.NormalizeForComparison
            // so "8069993037" and "08069993037" collide as the same motor mate.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_MotorMates_PhoneNo_Normalized"
                ON "MotorMates" (right(regexp_replace("PhoneNo", '\D', '', 'g'), 10))
                WHERE "PhoneNo" IS NOT NULL AND "PhoneNo" <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"UX_MotorMates_PhoneNo_Normalized\";");
        }
    }
}
