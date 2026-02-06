using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "program_id",
                table: "workouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "programs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    goal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    template = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    duration_weeks = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_programs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_programs__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_phases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    duration_weeks = table.Column<int>(type: "integer", nullable: false),
                    volume_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    intensity_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    target_rir = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_program_phases", x => x.id);
                    table.ForeignKey(
                        name: "f_k_program_phases_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "muscle_volume_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_phase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    muscle_group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    min_sets_per_week = table.Column<int>(type: "integer", nullable: false),
                    target_sets_per_week = table.Column<int>(type: "integer", nullable: false),
                    max_sets_per_week = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_muscle_volume_targets", x => x.id);
                    table.ForeignKey(
                        name: "f_k_muscle_volume_targets__program_phases_program_phase_id",
                        column: x => x.program_phase_id,
                        principalTable: "program_phases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_workouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_phase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    focus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_program_workouts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_program_workouts_program_phases_program_phase_id",
                        column: x => x.program_phase_id,
                        principalTable: "program_phases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    target_sets = table.Column<int>(type: "integer", nullable: false),
                    target_reps_min = table.Column<int>(type: "integer", nullable: false),
                    target_reps_max = table.Column<int>(type: "integer", nullable: false),
                    intensity_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    target_rpe = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    rest_seconds = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    superset_group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_program_exercises", x => x.id);
                    table.ForeignKey(
                        name: "f_k_program_exercises__program_workouts_program_workout_id",
                        column: x => x.program_workout_id,
                        principalTable: "program_workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_program_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_workouts_program_id",
                table: "workouts",
                column: "program_id",
                filter: "program_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_muscle_volume_targets_program_phase_id_muscle_group",
                table: "muscle_volume_targets",
                columns: new[] { "program_phase_id", "muscle_group" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_program_exercises_exercise_id",
                table: "program_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "i_x_program_exercises_program_workout_id_order_index",
                table: "program_exercises",
                columns: new[] { "program_workout_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "i_x_program_phases_program_id_order_index",
                table: "program_phases",
                columns: new[] { "program_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "i_x_program_workouts_program_phase_id_order_index",
                table: "program_workouts",
                columns: new[] { "program_phase_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "i_x_programs_user_id_status",
                table: "programs",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_programs_user_id_template",
                table: "programs",
                columns: new[] { "user_id", "template" });

            migrationBuilder.AddForeignKey(
                name: "f_k_workouts_programs_program_id",
                table: "workouts",
                column: "program_id",
                principalTable: "programs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_workouts_programs_program_id",
                table: "workouts");

            migrationBuilder.DropTable(
                name: "muscle_volume_targets");

            migrationBuilder.DropTable(
                name: "program_exercises");

            migrationBuilder.DropTable(
                name: "program_workouts");

            migrationBuilder.DropTable(
                name: "program_phases");

            migrationBuilder.DropTable(
                name: "programs");

            migrationBuilder.DropIndex(
                name: "i_x_workouts_program_id",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "program_id",
                table: "workouts");
        }
    }
}
