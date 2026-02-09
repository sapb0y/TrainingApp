using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TrainingApp.Core.Entities;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachAthleteAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coach_athletes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    athlete_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invite_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permissions = table.Column<CoachPermissions>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_coach_athletes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_coach_athletes__asp_net_users_athlete_id",
                        column: x => x.athlete_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_coach_athletes__asp_net_users_coach_id",
                        column: x => x.coach_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coach_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_athlete_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    athlete_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workout_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_coach_notes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_coach_notes__workout_sets_workout_set_id",
                        column: x => x.workout_set_id,
                        principalTable: "workout_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_coach_notes__workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_coach_notes_coach_athletes_coach_athlete_id",
                        column: x => x.coach_athlete_id,
                        principalTable: "coach_athletes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_coach_athletes_athlete_id_status",
                table: "coach_athletes",
                columns: new[] { "athlete_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_coach_athletes_coach_id_status",
                table: "coach_athletes",
                columns: new[] { "coach_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_coach_athletes_invite_code",
                table: "coach_athletes",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_coach_notes_athlete_id_created_at",
                table: "coach_notes",
                columns: new[] { "athlete_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_coach_notes_coach_athlete_id",
                table: "coach_notes",
                column: "coach_athlete_id");

            migrationBuilder.CreateIndex(
                name: "i_x_coach_notes_workout_id",
                table: "coach_notes",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "i_x_coach_notes_workout_set_id",
                table: "coach_notes",
                column: "workout_set_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coach_notes");

            migrationBuilder.DropTable(
                name: "coach_athletes");
        }
    }
}
