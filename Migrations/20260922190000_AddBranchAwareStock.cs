using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GreenRetail.Data;
#nullable disable
namespace GreenRetail.Migrations;
[DbContext(typeof(PosDbContext))]
[Migration("20260922190000_AddBranchAwareStock")]
public partial class AddBranchAwareStock : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("PRAGMA foreign_keys=OFF;");
        m.Sql("ALTER TABLE StockLevels RENAME TO StockLevels_Legacy;");
        m.Sql("ALTER TABLE StockLedger RENAME TO StockLedger_Legacy;");
        m.CreateTable("StockLevels", t => new { Id=t.Column<Guid>("TEXT",false), ProductId=t.Column<Guid>("TEXT",false), BranchId=t.Column<Guid>("TEXT",false), Quantity=t.Column<decimal>("TEXT",false) }, c=>{ c.PrimaryKey("PK_StockLevels",x=>x.Id); c.ForeignKey("FK_StockLevels_Products_ProductId",x=>x.ProductId,"Products","Id",ReferentialAction.Restrict); c.ForeignKey("FK_StockLevels_Branches_BranchId",x=>x.BranchId,"Branches","Id",ReferentialAction.Restrict); });
        m.CreateTable("StockLedger", t => new { Id=t.Column<Guid>("TEXT",false), ProductId=t.Column<Guid>("TEXT",false), BranchId=t.Column<Guid>("TEXT",false), QuantityChange=t.Column<decimal>("TEXT",false), Reason=t.Column<int>("INTEGER",false), Note=t.Column<string>("TEXT",true), CreatedUtc=t.Column<DateTime>("TEXT",false) }, c=>{ c.PrimaryKey("PK_StockLedger",x=>x.Id); c.ForeignKey("FK_StockLedger_Products_ProductId",x=>x.ProductId,"Products","Id",ReferentialAction.Restrict); c.ForeignKey("FK_StockLedger_Branches_BranchId",x=>x.BranchId,"Branches","Id",ReferentialAction.Restrict); });
        m.Sql("INSERT INTO StockLevels (Id,ProductId,BranchId,Quantity) SELECT s.Id,s.ProductId,(SELECT Id FROM Branches WHERE IsActive=1 ORDER BY CreatedUtc LIMIT 1),s.Quantity FROM StockLevels_Legacy s WHERE EXISTS (SELECT 1 FROM Branches WHERE IsActive=1);");
        m.Sql("INSERT INTO StockLedger (Id,ProductId,BranchId,QuantityChange,Reason,Note,CreatedUtc) SELECT s.Id,s.ProductId,(SELECT Id FROM Branches WHERE IsActive=1 ORDER BY CreatedUtc LIMIT 1),s.QuantityChange,s.Reason,s.Note,s.CreatedUtc FROM StockLedger_Legacy s WHERE EXISTS (SELECT 1 FROM Branches WHERE IsActive=1);");
        m.DropTable("StockLevels_Legacy"); m.DropTable("StockLedger_Legacy");
        m.CreateIndex("IX_StockLevels_BranchId_ProductId","StockLevels",new[]{"BranchId","ProductId"},true);
        m.CreateIndex("IX_StockLevels_BranchId","StockLevels","BranchId");
        m.CreateIndex("IX_StockLevels_ProductId","StockLevels","ProductId");
        m.CreateIndex("IX_StockLedger_BranchId","StockLedger","BranchId");
        m.CreateIndex("IX_StockLedger_ProductId","StockLedger","ProductId");
        m.Sql("PRAGMA foreign_keys=ON;");
    }
    protected override void Down(MigrationBuilder m) => throw new NotSupportedException("Branch-aware stock migration is intentionally irreversible.");
}
