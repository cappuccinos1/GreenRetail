using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GreenRetail.Data;

#nullable disable

namespace GreenRetail.Migrations;

[DbContext(typeof(PosDbContext))]
[Migration("20260922190000_AddBranchAwareStock")]
public partial class AddBranchAwareStock : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("PRAGMA foreign_keys=OFF;");

        migrationBuilder.Sql("ALTER TABLE StockLevels RENAME TO StockLevels_Legacy;");
        migrationBuilder.Sql("ALTER TABLE StockLedger RENAME TO StockLedger_Legacy;");

        migrationBuilder.CreateTable(
            name: "StockLevels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                BranchId = table.Column<Guid>(type: "TEXT", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockLevels", x => x.Id);
                table.ForeignKey("FK_StockLevels_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_StockLevels_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockLedger",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                BranchId = table.Column<Guid>(type: "TEXT", nullable: false),
                QuantityChange = table.Column<decimal>(type: "TEXT", nullable: false),
                Reason = table.Column<int>(type: "INTEGER", nullable: false),
                Note = table.Column<string>(type: "TEXT", nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockLedger", x => x.Id);
                table.ForeignKey("FK_StockLedger_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_StockLedger_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql("INSERT INTO StockLevels (Id, ProductId, BranchId, Quantity) SELECT s.Id, s.ProductId, (SELECT Id FROM Branches WHERE IsActive = 1 ORDER BY CreatedUtc LIMIT 1), s.Quantity FROM StockLevels_Legacy s WHERE EXISTS (SELECT 1 FROM Branches WHERE IsActive = 1);");
        migrationBuilder.Sql("INSERT INTO StockLedger (Id, ProductId, BranchId, QuantityChange, Reason, Note, CreatedUtc) SELECT s.Id, s.ProductId, (SELECT Id FROM Branches WHERE IsActive = 1 ORDER BY CreatedUtc LIMIT 1), s.QuantityChange, s.Reason, s.Note, s.CreatedUtc FROM StockLedger_Legacy s WHERE EXISTS (SELECT 1 FROM Branches WHERE IsActive = 1);");

        migrationBuilder.DropTable(name: "StockLevels_Legacy");
        migrationBuilder.DropTable(name: "StockLedger_Legacy");

        migrationBuilder.CreateIndex(
            name: "IX_StockLevels_BranchId_ProductId",
            table: "StockLevels",
            columns: new[] { "BranchId", "ProductId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_StockLevels_BranchId",
            table: "StockLevels",
            column: "BranchId");
        migrationBuilder.CreateIndex(
            name: "IX_StockLevels_ProductId",
            table: "StockLevels",
            column: "ProductId");
        migrationBuilder.CreateIndex(
            name: "IX_StockLedger_BranchId",
            table: "StockLedger",
            column: "BranchId");
        migrationBuilder.CreateIndex(
            name: "IX_StockLedger_ProductId",
            table: "StockLedger",
            column: "ProductId");

        migrationBuilder.Sql("PRAGMA foreign_keys=ON;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Branch-aware stock migration is intentionally irreversible.");
}
