using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GreenRetail.Data;

#nullable disable

namespace GreenRetail.Migrations;

[DbContext(typeof(PosDbContext))]
[Migration("20260922200000_AddReceivingQcStaging")]
public partial class AddReceivingQcStaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReceivingSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Number = table.Column<string>(type: "TEXT", nullable: false),
                PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                BranchId = table.Column<Guid>(type: "TEXT", nullable: false),
                VendorInvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                VendorInvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ReceivedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReceivedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                InspectionCompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                InspectedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                PostedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                PostedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                NoPoBuyerConfirmedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                NoPoBuyerConfirmedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReceivingSessions", x => x.Id);
                table.ForeignKey("FK_ReceivingSessions_PurchaseOrders_PurchaseOrderId", x => x.PurchaseOrderId, "PurchaseOrders", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ReceivingSessions_Suppliers_SupplierId", x => x.SupplierId, "Suppliers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ReceivingSessions_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ReceivingLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ReceivingSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                DeliveredQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                AcceptedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                RejectedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                UnitCostKobo = table.Column<long>(type: "INTEGER", nullable: false),
                RejectReason = table.Column<string>(type: "TEXT", nullable: true),
                ExpiryUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                BatchNumber = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReceivingLines", x => x.Id);
                table.ForeignKey("FK_ReceivingLines_ReceivingSessions_ReceivingSessionId", x => x.ReceivingSessionId, "ReceivingSessions", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ReceivingLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReceivingSessions_Number",
            table: "ReceivingSessions",
            column: "Number",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_ReceivingSessions_BranchId_Status",
            table: "ReceivingSessions",
            columns: new[] { "BranchId", "Status" });
        migrationBuilder.CreateIndex(
            name: "IX_ReceivingSessions_PurchaseOrderId",
            table: "ReceivingSessions",
            column: "PurchaseOrderId");
        migrationBuilder.CreateIndex(
            name: "IX_ReceivingSessions_SupplierId",
            table: "ReceivingSessions",
            column: "SupplierId");
        migrationBuilder.CreateIndex(
            name: "IX_ReceivingLines_ReceivingSessionId_ProductId",
            table: "ReceivingLines",
            columns: new[] { "ReceivingSessionId", "ProductId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_ReceivingLines_ProductId",
            table: "ReceivingLines",
            column: "ProductId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ReceivingLines");
        migrationBuilder.DropTable(name: "ReceivingSessions");
    }
}
