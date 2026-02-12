using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "exercises",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "exercises");
        }
    }
}
