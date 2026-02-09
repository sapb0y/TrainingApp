using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        private static readonly Guid AdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid CoachRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        private static readonly Guid AthleteRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "asp_net_roles",
                columns: new[] { "id", "name", "normalized_name", "concurrency_stamp" },
                values: new object[,]
                {
                    { AdminRoleId, "Admin", "ADMIN", Guid.NewGuid().ToString() },
                    { CoachRoleId, "Coach", "COACH", Guid.NewGuid().ToString() },
                    { AthleteRoleId, "Athlete", "ATHLETE", Guid.NewGuid().ToString() }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "asp_net_roles",
                keyColumn: "id",
                keyValues: new object[] { AdminRoleId, CoachRoleId, AthleteRoleId });
        }
    }
}
