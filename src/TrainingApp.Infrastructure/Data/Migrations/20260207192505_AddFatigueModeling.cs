using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFatigueModeling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    trimp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ctl = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    atl = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tsb = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_sets = table.Column<int>(type: "integer", nullable: false),
                    total_reps = table.Column<int>(type: "integer", nullable: false),
                    total_volume = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    workout_count = table.Column<int>(type: "integer", nullable: false),
                    average_session_rpe = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    readiness_score = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_daily_metrics", x => x.id);
                    table.ForeignKey(
                        name: "f_k_daily_metrics__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recovery_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    sleep_quality = table.Column<int>(type: "integer", nullable: true),
                    sleep_hours = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    stress_level = table.Column<int>(type: "integer", nullable: true),
                    energy_level = table.Column<int>(type: "integer", nullable: true),
                    muscle_readiness = table.Column<int>(type: "integer", nullable: true),
                    mood = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_recovery_logs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_recovery_logs__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_daily_metrics_user_id_date",
                table: "daily_metrics",
                columns: new[] { "user_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_recovery_logs_user_id_date",
                table: "recovery_logs",
                columns: new[] { "user_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_metrics");

            migrationBuilder.DropTable(
                name: "recovery_logs");
        }
    }
}
