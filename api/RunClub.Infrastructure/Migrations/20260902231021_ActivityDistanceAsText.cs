using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActivityDistanceAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DistanceMiles",
                table: "Activities",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "DistanceMiles",
                table: "Activities",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);
        }
    }
}
