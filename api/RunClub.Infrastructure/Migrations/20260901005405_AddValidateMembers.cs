using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddValidateMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnglandAthleticsNumber",
                table: "MemberProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ValidateMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    EnglandAthleticsNumber = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ClaimedUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidateMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidateMembers_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_EnglandAthleticsNumber",
                table: "MemberProfiles",
                column: "EnglandAthleticsNumber",
                unique: true,
                filter: "\"EnglandAthleticsNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ValidateMembers_ClubId_EnglandAthleticsNumber",
                table: "ValidateMembers",
                columns: new[] { "ClubId", "EnglandAthleticsNumber" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"ClaimedUserId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidateMembers");

            migrationBuilder.DropIndex(
                name: "IX_MemberProfiles_EnglandAthleticsNumber",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "EnglandAthleticsNumber",
                table: "MemberProfiles");
        }
    }
}
