using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FTLSV2.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.CreateTable(
                name: "faculty_load_summary",
                columns: table => new
                {
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_load = table.Column<decimal>(type: "numeric", nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                });*/

            migrationBuilder.CreateTable(
                name: "room_registry",
                columns: table => new
                {
                    room_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    availability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_registry", x => x.room_id);
                });

            migrationBuilder.CreateTable(
                name: "subject",
                columns: table => new
                {
                    subject_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    units = table.Column<int>(type: "integer", nullable: false),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    semester = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject", x => x.subject_id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxOverload = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    faculty_id = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_units = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    faculty_id = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<int>(type: "integer", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    time_slot = table.Column<string>(type: "text", nullable: false),
                    assigned_units = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK_schedule_room_registry_room_id",
                        column: x => x.room_id,
                        principalTable: "room_registry",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "subject_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_users_faculty_id",
                        column: x => x.faculty_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_faculty_id",
                table: "schedule",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_room_id",
                table: "schedule",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_subject_id",
                table: "schedule",
                column: "subject_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faculty_load_summary");

            migrationBuilder.DropTable(
                name: "schedule");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "room_registry");

            migrationBuilder.DropTable(
                name: "subject");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
