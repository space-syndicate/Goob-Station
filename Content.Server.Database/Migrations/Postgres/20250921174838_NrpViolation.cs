using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class NrpViolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "language",
                columns: table => new
                {
                    language_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    language_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language", x => x.language_id);
                    table.ForeignKey(
                        name: "FK_language_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nrp_resolves",
                columns: table => new
                {
                    nrp_resolves_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rp = table.Column<int>(type: "integer", nullable: false),
                    nrp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrp_resolves", x => x.nrp_resolves_id);
                });

            migrationBuilder.CreateTable(
                name: "nrp_violation",
                columns: table => new
                {
                    nrp_violation_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    violation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrp_violation", x => x.nrp_violation_id);
                });

            migrationBuilder.CreateTable(
                name: "skill",
                columns: table => new
                {
                    skill_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    skill_name = table.Column<string>(type: "text", nullable: false),
                    skill_level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill", x => x.skill_id);
                    table.ForeignKey(
                        name: "FK_skill_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_language_profile_id_language_name",
                table: "language",
                columns: new[] { "profile_id", "language_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nrp_resolves_user_id",
                table: "nrp_resolves",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_nrp_violation_user_id",
                table: "nrp_violation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_skill_profile_id_skill_name",
                table: "skill",
                columns: new[] { "profile_id", "skill_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "language");

            migrationBuilder.DropTable(
                name: "nrp_resolves");

            migrationBuilder.DropTable(
                name: "nrp_violation");

            migrationBuilder.DropTable(
                name: "skill");
        }
    }
}
