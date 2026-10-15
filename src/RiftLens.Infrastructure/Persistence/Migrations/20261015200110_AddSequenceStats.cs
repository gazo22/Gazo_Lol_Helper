using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SequenceStats",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Key = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceStats", x => new { x.Patch, x.Bucket, x.ChampionId, x.Role, x.Kind, x.Key });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SequenceStats",
                schema: "rl");
        }
    }
}
