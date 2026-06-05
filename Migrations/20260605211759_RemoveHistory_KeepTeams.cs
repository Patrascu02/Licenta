using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licenta.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHistory_KeepTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_CurrentTeamId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "PlayerTeamHistories");

            migrationBuilder.DropIndex(
                name: "IX_Players_CurrentTeamId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CurrentTeamId",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamId",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamId",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Games_AwayTeamId",
                table: "Games",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games",
                column: "HomeTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_AwayTeamId",
                table: "Games",
                column: "AwayTeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_HomeTeamId",
                table: "Games",
                column: "HomeTeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Teams_AwayTeamId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Teams_HomeTeamId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_AwayTeamId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AwayTeamId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HomeTeamId",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "CurrentTeamId",
                table: "Players",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayerTeamHistories",
                columns: table => new
                {
                    PlayerTeamHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTeamHistories", x => x.PlayerTeamHistoryId);
                    table.ForeignKey(
                        name: "FK_PlayerTeamHistories_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTeamHistories_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_CurrentTeamId",
                table: "Players",
                column: "CurrentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamHistories_PlayerId",
                table: "PlayerTeamHistories",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamHistories_TeamId",
                table: "PlayerTeamHistories",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_CurrentTeamId",
                table: "Players",
                column: "CurrentTeamId",
                principalTable: "Teams",
                principalColumn: "TeamId");
        }
    }
}
