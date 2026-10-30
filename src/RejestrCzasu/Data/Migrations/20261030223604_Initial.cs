using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RejestrCzasu.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_entry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    work_type_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_entry_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_time_entry_work_type_work_type_id",
                        column: x => x.work_type_id,
                        principalTable: "work_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_code",
                table: "project",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_entry_date",
                table: "time_entry",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_time_entry_project_id",
                table: "time_entry",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_entry_work_type_id",
                table: "time_entry",
                column: "work_type_id");

            // AD-7: work type name unique regardless of letter case.
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_work_type_name_lower ON work_type (lower(name));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_work_type_name_lower;");

            migrationBuilder.DropTable(
                name: "time_entry");

            migrationBuilder.DropTable(
                name: "project");

            migrationBuilder.DropTable(
                name: "work_type");
        }
    }
}
