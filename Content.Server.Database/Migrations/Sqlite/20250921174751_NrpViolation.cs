using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
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
                    language_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    language_name = table.Column<string>(type: "TEXT", nullable: false)
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
                    nrp_resolves_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    rp = table.Column<int>(type: "INTEGER", nullable: false),
                    nrp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrp_resolves", x => x.nrp_resolves_id);
                });

            migrationBuilder.CreateTable(
                name: "nrp_violation",
                columns: table => new
                {
                    nrp_violation_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    violation_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nrp_violation", x => x.nrp_violation_id);
                });

            migrationBuilder.CreateTable(
                name: "skill",
                columns: table => new
                {
                    skill_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    skill_name = table.Column<string>(type: "TEXT", nullable: false),
                    skill_level = table.Column<int>(type: "INTEGER", nullable: false)
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
