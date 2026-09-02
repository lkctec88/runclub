using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActivityRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecurrenceFrequency",
                table: "Activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceGroupId",
                table: "Activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceUntilUtc",
                table: "Activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_RecurrenceGroupId",
                table: "Activities",
                column: "RecurrenceGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Activities_RecurrenceGroupId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "RecurrenceFrequency",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "RecurrenceGroupId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "RecurrenceUntilUtc",
                table: "Activities");
        }
    }
}
