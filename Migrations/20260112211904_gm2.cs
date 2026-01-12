using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licenta.Migrations
{
    /// <inheritdoc />
    public partial class gm2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_GeneralManager_GeneralManagerId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_GeneralManager_Staff_StaffId",
                table: "GeneralManager");

            migrationBuilder.DropForeignKey(
                name: "FK_TerminationNotices_GeneralManager_GeneralManagerId",
                table: "TerminationNotices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralManager",
                table: "GeneralManager");

            migrationBuilder.RenameTable(
                name: "GeneralManager",
                newName: "GeneralManagers");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralManager_StaffId",
                table: "GeneralManagers",
                newName: "IX_GeneralManagers_StaffId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralManagers",
                table: "GeneralManagers",
                column: "GeneralManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_GeneralManagers_GeneralManagerId",
                table: "Expenses",
                column: "GeneralManagerId",
                principalTable: "GeneralManagers",
                principalColumn: "GeneralManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GeneralManagers_Staff_StaffId",
                table: "GeneralManagers",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TerminationNotices_GeneralManagers_GeneralManagerId",
                table: "TerminationNotices",
                column: "GeneralManagerId",
                principalTable: "GeneralManagers",
                principalColumn: "GeneralManagerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_GeneralManagers_GeneralManagerId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_GeneralManagers_Staff_StaffId",
                table: "GeneralManagers");

            migrationBuilder.DropForeignKey(
                name: "FK_TerminationNotices_GeneralManagers_GeneralManagerId",
                table: "TerminationNotices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralManagers",
                table: "GeneralManagers");

            migrationBuilder.RenameTable(
                name: "GeneralManagers",
                newName: "GeneralManager");

            migrationBuilder.RenameIndex(
                name: "IX_GeneralManagers_StaffId",
                table: "GeneralManager",
                newName: "IX_GeneralManager_StaffId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralManager",
                table: "GeneralManager",
                column: "GeneralManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_GeneralManager_GeneralManagerId",
                table: "Expenses",
                column: "GeneralManagerId",
                principalTable: "GeneralManager",
                principalColumn: "GeneralManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GeneralManager_Staff_StaffId",
                table: "GeneralManager",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TerminationNotices_GeneralManager_GeneralManagerId",
                table: "TerminationNotices",
                column: "GeneralManagerId",
                principalTable: "GeneralManager",
                principalColumn: "GeneralManagerId");
        }
    }
}
