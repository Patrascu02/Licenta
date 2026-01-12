using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licenta.Migrations
{
    /// <inheritdoc />
    public partial class gm1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Players_PlayerId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_PlayerId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Contracts");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Contracts",
                newName: "StaffId");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetLimit",
                table: "Teams",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_StaffId",
                table: "Contracts",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Staff_StaffId",
                table: "Contracts",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Staff_StaffId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_StaffId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "BudgetLimit",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "Contracts",
                newName: "Version");

            migrationBuilder.AddColumn<int>(
                name: "PlayerId",
                table: "Contracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PlayerId",
                table: "Contracts",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Players_PlayerId",
                table: "Contracts",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
