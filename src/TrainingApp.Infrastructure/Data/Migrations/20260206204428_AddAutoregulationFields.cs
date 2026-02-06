using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoregulationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "post_workout_fatigue",
                table: "workouts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pre_workout_readiness",
                table: "workouts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "adjustment_reason",
                table: "workout_sets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "target_rir",
                table: "workout_sets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "was_auto_adjusted",
                table: "workout_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "adaptation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    input_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    output_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    was_applied = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_adaptation_logs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_adaptation_logs__workout_sets_workout_set_id",
                        column: x => x.workout_set_id,
                        principalTable: "workout_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_adaptation_logs__workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_adaptation_logs_workout_id_created_at",
                table: "adaptation_logs",
                columns: new[] { "workout_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_adaptation_logs_workout_set_id",
                table: "adaptation_logs",
                column: "workout_set_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adaptation_logs");

            migrationBuilder.DropColumn(
                name: "post_workout_fatigue",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "pre_workout_readiness",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "adjustment_reason",
                table: "workout_sets");

            migrationBuilder.DropColumn(
                name: "target_rir",
                table: "workout_sets");

            migrationBuilder.DropColumn(
                name: "was_auto_adjusted",
                table: "workout_sets");
        }
    }
}
