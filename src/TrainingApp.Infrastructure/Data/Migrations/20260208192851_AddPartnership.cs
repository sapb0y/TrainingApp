using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partnerships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invite_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_partnerships", x => x.id);
                    table.ForeignKey(
                        name: "f_k_partnerships__asp_net_users_requester_id",
                        column: x => x.requester_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_partnerships__asp_net_users_responder_id",
                        column: x => x.responder_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shared_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partnership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    workout_a_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workout_b_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    solo_estimate_minutes_a = table.Column<int>(type: "integer", nullable: true),
                    solo_estimate_minutes_b = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_shared_sessions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_shared_sessions__workouts_workout_a_id",
                        column: x => x.workout_a_id,
                        principalTable: "workouts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "f_k_shared_sessions__workouts_workout_b_id",
                        column: x => x.workout_b_id,
                        principalTable: "workouts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "f_k_shared_sessions_partnerships_partnership_id",
                        column: x => x.partnership_id,
                        principalTable: "partnerships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shared_session_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shared_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_order = table.Column<int>(type: "integer", nullable: false),
                    user_a_exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_a_set_number = table.Column<int>(type: "integer", nullable: true),
                    user_a_action = table.Column<string>(type: "text", nullable: true),
                    user_b_exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_b_set_number = table.Column<int>(type: "integer", nullable: true),
                    user_b_action = table.Column<string>(type: "text", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    is_parallel = table.Column<bool>(type: "boolean", nullable: false),
                    equipment_note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_shared_session_slots", x => x.id);
                    table.ForeignKey(
                        name: "f_k_shared_session_slots_shared_sessions_shared_session_id",
                        column: x => x.shared_session_id,
                        principalTable: "shared_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_partnerships_invite_code",
                table: "partnerships",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_partnerships_requester_id_status",
                table: "partnerships",
                columns: new[] { "requester_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_partnerships_responder_id_status",
                table: "partnerships",
                columns: new[] { "responder_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_shared_session_slots_shared_session_id",
                table: "shared_session_slots",
                column: "shared_session_id");

            migrationBuilder.CreateIndex(
                name: "i_x_shared_sessions_partnership_id",
                table: "shared_sessions",
                column: "partnership_id");

            migrationBuilder.CreateIndex(
                name: "i_x_shared_sessions_workout_a_id",
                table: "shared_sessions",
                column: "workout_a_id");

            migrationBuilder.CreateIndex(
                name: "i_x_shared_sessions_workout_b_id",
                table: "shared_sessions",
                column: "workout_b_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shared_session_slots");

            migrationBuilder.DropTable(
                name: "shared_sessions");

            migrationBuilder.DropTable(
                name: "partnerships");
        }
    }
}
