using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenRetail.Migrations;

public partial class AddTerminalBranchScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "BranchId",
            table: "Terminals",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Terminals_BranchId",
            table: "Terminals",
            column: "BranchId");

        migrationBuilder.AddForeignKey(
            name: "FK_Terminals_Branches_BranchId",
            table: "Terminals",
            column: "BranchId",
            principalTable: "Branches",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Terminals_Branches_BranchId",
            table: "Terminals");

        migrationBuilder.DropIndex(
            name: "IX_Terminals_BranchId",
            table: "Terminals");

        migrationBuilder.DropColumn(
            name: "BranchId",
            table: "Terminals");
    }
}
