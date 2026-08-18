using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTLSV2.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyTypeToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new faculty employment classification.
            migrationBuilder.AddColumn<string>(
                name: "faculty_type",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Existing Faculty and Chairman accounts are treated as Full-Time.
            // Full-Time faculty currently use a maximum teaching load of 30 units.
            migrationBuilder.Sql("""
                UPDATE users
                SET faculty_type = 'Full-Time',
                    max_units = 30
                WHERE role IN ('Faculty', 'Chairman');
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "faculty_type",
                table: "users");
        }
    }
}