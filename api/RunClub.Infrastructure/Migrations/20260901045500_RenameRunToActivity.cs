using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameRunToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "RunId", table: "VolunteerSlots", newName: "ActivityId");
            migrationBuilder.RenameColumn(name: "RunId", table: "TrainingParticipations", newName: "ActivityId");
            migrationBuilder.RenameColumn(name: "RunId", table: "RunAttendances", newName: "ActivityId");
            migrationBuilder.RenameColumn(name: "RunId", table: "RunCheckIns", newName: "ActivityId");
            migrationBuilder.RenameColumn(name: "RunId", table: "RunCheckOuts", newName: "ActivityId");
            migrationBuilder.RenameColumn(name: "RunId", table: "RunRatings", newName: "ActivityId");
            migrationBuilder.RenameColumn(name: "RunsCompleted", table: "MemberProfiles", newName: "ActivitiesCompleted");
            migrationBuilder.RenameColumn(name: "RunsLed", table: "MemberProfiles", newName: "ActivitiesLed");

            migrationBuilder.RenameTable(name: "Runs", newName: "Activities");
            migrationBuilder.RenameTable(name: "RunAttendances", newName: "ActivityAttendances");
            migrationBuilder.RenameTable(name: "RunCheckIns", newName: "ActivityCheckIns");
            migrationBuilder.RenameTable(name: "RunCheckOuts", newName: "ActivityCheckOuts");
            migrationBuilder.RenameTable(name: "RunRatings", newName: "ActivityRatings");

            migrationBuilder.RenameIndex(name: "IX_Runs_ClubId", table: "Activities", newName: "IX_Activities_ClubId");
            migrationBuilder.RenameIndex(name: "IX_RunAttendances_RunId_UserId", table: "ActivityAttendances", newName: "IX_ActivityAttendances_ActivityId_UserId");
            migrationBuilder.RenameIndex(name: "IX_RunCheckIns_RunId_UserId", table: "ActivityCheckIns", newName: "IX_ActivityCheckIns_ActivityId_UserId");
            migrationBuilder.RenameIndex(name: "IX_RunCheckOuts_RunId_UserId", table: "ActivityCheckOuts", newName: "IX_ActivityCheckOuts_ActivityId_UserId");
            migrationBuilder.RenameIndex(name: "IX_TrainingParticipations_RunId", table: "TrainingParticipations", newName: "IX_TrainingParticipations_ActivityId");
            migrationBuilder.RenameIndex(name: "IX_VolunteerSlots_RunId", table: "VolunteerSlots", newName: "IX_VolunteerSlots_ActivityId");

            migrationBuilder.Sql("""ALTER TABLE "Activities" RENAME CONSTRAINT "PK_Runs" TO "PK_Activities";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityAttendances" RENAME CONSTRAINT "PK_RunAttendances" TO "PK_ActivityAttendances";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckIns" RENAME CONSTRAINT "PK_RunCheckIns" TO "PK_ActivityCheckIns";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckOuts" RENAME CONSTRAINT "PK_RunCheckOuts" TO "PK_ActivityCheckOuts";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityRatings" RENAME CONSTRAINT "PK_RunRatings" TO "PK_ActivityRatings";""");

            migrationBuilder.Sql("""ALTER TABLE "Activities" RENAME CONSTRAINT "FK_Runs_Clubs_ClubId" TO "FK_Activities_Clubs_ClubId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityAttendances" RENAME CONSTRAINT "FK_RunAttendances_Runs_RunId" TO "FK_ActivityAttendances_Activities_ActivityId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckIns" RENAME CONSTRAINT "FK_RunCheckIns_Runs_RunId" TO "FK_ActivityCheckIns_Activities_ActivityId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckOuts" RENAME CONSTRAINT "FK_RunCheckOuts_Runs_RunId" TO "FK_ActivityCheckOuts_Activities_ActivityId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityRatings" RENAME CONSTRAINT "FK_RunRatings_Runs_RunId" TO "FK_ActivityRatings_Activities_ActivityId";""");
            migrationBuilder.Sql("""ALTER TABLE "TrainingParticipations" RENAME CONSTRAINT "FK_TrainingParticipations_Runs_RunId" TO "FK_TrainingParticipations_Activities_ActivityId";""");
            migrationBuilder.Sql("""ALTER TABLE "VolunteerSlots" RENAME CONSTRAINT "FK_VolunteerSlots_Runs_RunId" TO "FK_VolunteerSlots_Activities_ActivityId";""");

            migrationBuilder.DropIndex(name: "IX_RunRatings_RunId", table: "ActivityRatings");
            migrationBuilder.CreateIndex(
                name: "IX_ActivityRatings_ActivityId_UserId",
                table: "ActivityRatings",
                columns: new[] { "ActivityId", "UserId" },
                unique: true);

            migrationBuilder.AddColumn<bool>(
                name: "RatingSkipped",
                table: "ActivityAttendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RatingSkipped", table: "ActivityAttendances");

            migrationBuilder.DropIndex(name: "IX_ActivityRatings_ActivityId_UserId", table: "ActivityRatings");
            migrationBuilder.CreateIndex(
                name: "IX_RunRatings_RunId",
                table: "ActivityRatings",
                column: "ActivityId");

            migrationBuilder.Sql("""ALTER TABLE "VolunteerSlots" RENAME CONSTRAINT "FK_VolunteerSlots_Activities_ActivityId" TO "FK_VolunteerSlots_Runs_RunId";""");
            migrationBuilder.Sql("""ALTER TABLE "TrainingParticipations" RENAME CONSTRAINT "FK_TrainingParticipations_Activities_ActivityId" TO "FK_TrainingParticipations_Runs_RunId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityRatings" RENAME CONSTRAINT "FK_ActivityRatings_Activities_ActivityId" TO "FK_RunRatings_Runs_RunId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckOuts" RENAME CONSTRAINT "FK_ActivityCheckOuts_Activities_ActivityId" TO "FK_RunCheckOuts_Runs_RunId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckIns" RENAME CONSTRAINT "FK_ActivityCheckIns_Activities_ActivityId" TO "FK_RunCheckIns_Runs_RunId";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityAttendances" RENAME CONSTRAINT "FK_ActivityAttendances_Activities_ActivityId" TO "FK_RunAttendances_Runs_RunId";""");
            migrationBuilder.Sql("""ALTER TABLE "Activities" RENAME CONSTRAINT "FK_Activities_Clubs_ClubId" TO "FK_Runs_Clubs_ClubId";""");

            migrationBuilder.Sql("""ALTER TABLE "ActivityRatings" RENAME CONSTRAINT "PK_ActivityRatings" TO "PK_RunRatings";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckOuts" RENAME CONSTRAINT "PK_ActivityCheckOuts" TO "PK_RunCheckOuts";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityCheckIns" RENAME CONSTRAINT "PK_ActivityCheckIns" TO "PK_RunCheckIns";""");
            migrationBuilder.Sql("""ALTER TABLE "ActivityAttendances" RENAME CONSTRAINT "PK_ActivityAttendances" TO "PK_RunAttendances";""");
            migrationBuilder.Sql("""ALTER TABLE "Activities" RENAME CONSTRAINT "PK_Activities" TO "PK_Runs";""");

            migrationBuilder.RenameIndex(name: "IX_VolunteerSlots_ActivityId", table: "VolunteerSlots", newName: "IX_VolunteerSlots_RunId");
            migrationBuilder.RenameIndex(name: "IX_TrainingParticipations_ActivityId", table: "TrainingParticipations", newName: "IX_TrainingParticipations_RunId");
            migrationBuilder.RenameIndex(name: "IX_ActivityCheckOuts_ActivityId_UserId", table: "ActivityCheckOuts", newName: "IX_RunCheckOuts_RunId_UserId");
            migrationBuilder.RenameIndex(name: "IX_ActivityCheckIns_ActivityId_UserId", table: "ActivityCheckIns", newName: "IX_RunCheckIns_RunId_UserId");
            migrationBuilder.RenameIndex(name: "IX_ActivityAttendances_ActivityId_UserId", table: "ActivityAttendances", newName: "IX_RunAttendances_RunId_UserId");
            migrationBuilder.RenameIndex(name: "IX_Activities_ClubId", table: "Activities", newName: "IX_Runs_ClubId");

            migrationBuilder.RenameTable(name: "Activities", newName: "Runs");
            migrationBuilder.RenameTable(name: "ActivityAttendances", newName: "RunAttendances");
            migrationBuilder.RenameTable(name: "ActivityCheckIns", newName: "RunCheckIns");
            migrationBuilder.RenameTable(name: "ActivityCheckOuts", newName: "RunCheckOuts");
            migrationBuilder.RenameTable(name: "ActivityRatings", newName: "RunRatings");

            migrationBuilder.RenameColumn(name: "ActivityId", table: "VolunteerSlots", newName: "RunId");
            migrationBuilder.RenameColumn(name: "ActivityId", table: "TrainingParticipations", newName: "RunId");
            migrationBuilder.RenameColumn(name: "ActivityId", table: "RunAttendances", newName: "RunId");
            migrationBuilder.RenameColumn(name: "ActivityId", table: "RunCheckIns", newName: "RunId");
            migrationBuilder.RenameColumn(name: "ActivityId", table: "RunCheckOuts", newName: "RunId");
            migrationBuilder.RenameColumn(name: "ActivityId", table: "RunRatings", newName: "RunId");
            migrationBuilder.RenameColumn(name: "ActivitiesCompleted", table: "MemberProfiles", newName: "RunsCompleted");
            migrationBuilder.RenameColumn(name: "ActivitiesLed", table: "MemberProfiles", newName: "RunsLed");
        }
    }
}
