using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rl");

            migrationBuilder.CreateTable(
                name: "Matches",
                schema: "rl",
                columns: table => new
                {
                    MatchId = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    QueueId = table.Column<int>(type: "int", nullable: false),
                    GameVersion = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PatchMajor = table.Column<int>(type: "int", nullable: false),
                    PatchMinor = table.Column<int>(type: "int", nullable: false),
                    GameStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsRemake = table.Column<bool>(type: "bit", nullable: false),
                    TimelineGzip = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                schema: "rl",
                columns: table => new
                {
                    Puuid = table.Column<string>(type: "varchar(78)", unicode: false, maxLength: 78, nullable: false),
                    GameName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TagLine = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ProfileIconId = table.Column<int>(type: "int", nullable: false),
                    SummonerLevel = table.Column<long>(type: "bigint", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Puuid);
                });

            migrationBuilder.CreateTable(
                name: "MatchBans",
                schema: "rl",
                columns: table => new
                {
                    MatchId = table.Column<string>(type: "varchar(32)", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    PickTurn = table.Column<int>(type: "int", nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchBans", x => new { x.MatchId, x.TeamId, x.PickTurn });
                    table.ForeignKey(
                        name: "FK_MatchBans_Matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "rl",
                        principalTable: "Matches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchParticipants",
                schema: "rl",
                columns: table => new
                {
                    MatchId = table.Column<string>(type: "varchar(32)", nullable: false),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    Puuid = table.Column<string>(type: "varchar(78)", unicode: false, maxLength: 78, nullable: false),
                    RiotIdGameName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RiotIdTagline = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    Win = table.Column<bool>(type: "bit", nullable: false),
                    ChampionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ChampLevel = table.Column<int>(type: "int", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    Deaths = table.Column<int>(type: "int", nullable: false),
                    Assists = table.Column<int>(type: "int", nullable: false),
                    TotalMinionsKilled = table.Column<int>(type: "int", nullable: false),
                    NeutralMinionsKilled = table.Column<int>(type: "int", nullable: false),
                    GoldEarned = table.Column<int>(type: "int", nullable: false),
                    TotalDamageDealtToChampions = table.Column<int>(type: "int", nullable: false),
                    DamageTaken = table.Column<int>(type: "int", nullable: false),
                    VisionScore = table.Column<int>(type: "int", nullable: false),
                    WardsPlaced = table.Column<int>(type: "int", nullable: false),
                    Items = table.Column<string>(type: "varchar(96)", unicode: false, maxLength: 96, nullable: false),
                    Summoners = table.Column<string>(type: "varchar(96)", unicode: false, maxLength: 96, nullable: false),
                    PrimaryStyle = table.Column<int>(type: "int", nullable: false),
                    SubStyle = table.Column<int>(type: "int", nullable: false),
                    Perks = table.Column<string>(type: "varchar(96)", unicode: false, maxLength: 96, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchParticipants", x => new { x.MatchId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_MatchParticipants_Matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "rl",
                        principalTable: "Matches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueSnapshots",
                schema: "rl",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Puuid = table.Column<string>(type: "varchar(78)", unicode: false, maxLength: 78, nullable: false),
                    Queue = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Division = table.Column<int>(type: "int", nullable: false),
                    LeaguePoints = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueSnapshots_Players_Puuid",
                        column: x => x.Puuid,
                        principalSchema: "rl",
                        principalTable: "Players",
                        principalColumn: "Puuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueSnapshots_Puuid_Queue_CapturedAt",
                schema: "rl",
                table: "LeagueSnapshots",
                columns: new[] { "Puuid", "Queue", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PatchMajor_PatchMinor_QueueId",
                schema: "rl",
                table: "Matches",
                columns: new[] { "PatchMajor", "PatchMinor", "QueueId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipants_ChampionId_Role",
                schema: "rl",
                table: "MatchParticipants",
                columns: new[] { "ChampionId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipants_Puuid",
                schema: "rl",
                table: "MatchParticipants",
                column: "Puuid");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameName_TagLine",
                schema: "rl",
                table: "Players",
                columns: new[] { "GameName", "TagLine" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueSnapshots",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "MatchBans",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "MatchParticipants",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "Players",
                schema: "rl");

            migrationBuilder.DropTable(
                name: "Matches",
                schema: "rl");
        }
    }
}
