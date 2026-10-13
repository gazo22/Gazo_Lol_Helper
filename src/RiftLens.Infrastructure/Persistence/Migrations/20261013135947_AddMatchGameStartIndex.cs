using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchGameStartIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Matches_GameStart",
                schema: "rl",
                table: "Matches",
                column: "GameStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_GameStart",
                schema: "rl",
                table: "Matches");
        }
    }
}
