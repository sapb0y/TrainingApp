using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_shared_sessions_partnership_id",
                table: "shared_sessions");

            migrationBuilder.DropIndex(
                name: "i_x_shared_session_slots_shared_session_id",
                table: "shared_session_slots");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "shared_sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "shared_sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                table: "shared_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "user_b_action",
                table: "shared_session_slots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_a_action",
                table: "shared_session_slots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "equipment_note",
                table: "shared_session_slots",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_shared_sessions_partnership_id_scheduled_date",
                table: "shared_sessions",
                columns: new[] { "partnership_id", "scheduled_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_shared_session_slots_shared_session_id_slot_order",
                table: "shared_session_slots",
                columns: new[] { "shared_session_id", "slot_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_shared_sessions_partnership_id_scheduled_date",
                table: "shared_sessions");

            migrationBuilder.DropIndex(
                name: "i_x_shared_session_slots_shared_session_id_slot_order",
                table: "shared_session_slots");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "shared_sessions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "shared_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                table: "shared_sessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "user_b_action",
                table: "shared_session_slots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_a_action",
                table: "shared_session_slots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "equipment_note",
                table: "shared_session_slots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_shared_sessions_partnership_id",
                table: "shared_sessions",
                column: "partnership_id");

            migrationBuilder.CreateIndex(
                name: "i_x_shared_session_slots_shared_session_id",
                table: "shared_session_slots",
                column: "shared_session_id");
        }
    }
}
