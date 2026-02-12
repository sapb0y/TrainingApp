using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutReviewAndCoachMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "coach_feedback",
                table: "workouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                table: "workouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_coach_id",
                table: "workouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "coach_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_athlete_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_coach_messages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_coach_messages__asp_net_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_coach_messages_coach_athletes_coach_athlete_id",
                        column: x => x.coach_athlete_id,
                        principalTable: "coach_athletes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_coach_messages_coach_athlete_id",
                table: "coach_messages",
                column: "coach_athlete_id");

            migrationBuilder.CreateIndex(
                name: "i_x_coach_messages_sender_id",
                table: "coach_messages",
                column: "sender_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coach_messages");

            migrationBuilder.DropColumn(
                name: "coach_feedback",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "reviewed_by_coach_id",
                table: "workouts");
        }
    }
}
