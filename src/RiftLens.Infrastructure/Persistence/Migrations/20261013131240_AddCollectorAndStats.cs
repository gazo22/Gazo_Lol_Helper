using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectorAndStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceTier",
                schema: "rl",
                table: "Matches",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BuildStats",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ItemsKey = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildStats", x => new { x.Patch, x.Bucket, x.ChampionId, x.Role, x.ItemsKey });
                });

            migrationBuilder.CreateTable(
                name: "ChampionBanStats",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Bans = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionBanStats", x => new { x.Patch, x.Bucket, x.ChampionId });
                });

            migrationBuilder.CreateTable(
                name: "ChampionStats",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionStats", x => new { x.Patch, x.Bucket, x.ChampionId, x.Role });
                });

            migrationBuilder.CreateTable(
                name: "CollectorPlayers",
                schema: "rl",
                columns: table => new
                {
                    Puuid = table.Column<string>(type: "varchar(78)", unicode: false, maxLength: 78, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LeaguePoints = table.Column<int>(type: "int", nullable: false),
                    SeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastCrawledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorPlayers", x => x.Puuid);
                });

            migrationBuilder.CreateTable(
                name: "MatchupStats",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    OpponentId = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchupStats", x => new { x.Patch, x.Bucket, x.Role, x.ChampionId, x.OpponentId });
                });

            migrationBuilder.CreateTable(
                name: "PatchTotals",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Matches = table.Column<int>(type: "int", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchTotals", x => new { x.Patch, x.Bucket });
                });

            migrationBuilder.CreateTable(
                name: "QueuedMatches",
                schema: "rl",
                columns: table => new
                {
                    MatchId = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SourceTier = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedMatches", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "RuneStats",
                schema: "rl",
                columns: table => new
                {
                    Patch = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PerksKey = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneStats", x => new { x.Patch, x.Bucket, x.ChampionId, x.Role, x.PerksKey });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectorPlayers_LastCrawledAt",
                schema: "rl",
                table: "CollectorPlayers",
                column: "LastCrawledAt");

            migrationBuilder.CreateIndex(
                name: "IX_QueuedMatches_Status_EnqueuedAt",
                schema: "rl",
                table: "QueuedMatches",
                columns: new[] { "Status", "EnqueuedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildStats",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "ChampionBanStats",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "ChampionStats",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "CollectorPlayers",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "MatchupStats",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "PatchTotals",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "QueuedMatches",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "RuneStats",
                schema: "rl");

            migrationBuilder.DropColumn(
                name: "SourceTier",
                schema: "rl",
                table: "Matches");
        }
    }
}
