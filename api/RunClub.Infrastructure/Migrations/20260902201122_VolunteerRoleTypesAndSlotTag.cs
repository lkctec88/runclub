using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VolunteerRoleTypesAndSlotTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "VolunteerSlots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VolunteerRoleTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerRoleTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerRoleTypes_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRoleTypes_ClubId_Name",
                table: "VolunteerRoleTypes",
                columns: new[] { "ClubId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerRoleTypes");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "VolunteerSlots");
        }
    }
}
