using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenRetail.Migrations
{
    /// <inheritdoc />
    public partial class MigrateFinancialsToKobo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceDue",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ChangeDue",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Paid",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Rounding",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Sales",
                newName: "TerminalId");

            migrationBuilder.RenameColumn(
                name: "Tendered",
                table: "Sales",
                newName: "IdempotencyKey");

            migrationBuilder.AddColumn<long>(
                name: "BalanceDueKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ChangeDueKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DiscountKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PaidKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RoundingKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubtotalKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TaxKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TenderedKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalKobo",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DiscountKobo",
                table: "SaleItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalKobo",
                table: "SaleItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UnitPriceKobo",
                table: "SaleItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AmountKobo",
                table: "Payments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terminals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceivedNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorInvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    VendorInvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ReceivedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceivedNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedNotes_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedNotes_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceivedNoteLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GoodsReceivedNoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    RejectReason = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiryUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BatchNumber = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceivedNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedNoteLines_GoodsReceivedNotes_GoodsReceivedNoteId",
                        column: x => x.GoodsReceivedNoteId,
                        principalTable: "GoodsReceivedNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedNoteLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_IdempotencyKey",
                table: "Sales",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TerminalId",
                table: "Sales",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedNoteLines_GoodsReceivedNoteId",
                table: "GoodsReceivedNoteLines",
                column: "GoodsReceivedNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedNoteLines_ProductId",
                table: "GoodsReceivedNoteLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedNotes_Number",
                table: "GoodsReceivedNotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedNotes_PurchaseOrderId",
                table: "GoodsReceivedNotes",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedNotes_SupplierId",
                table: "GoodsReceivedNotes",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProductId",
                table: "PurchaseOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Number",
                table: "PurchaseOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_Code",
                table: "Terminals",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Terminals_TerminalId",
                table: "Sales",
                column: "TerminalId",
                principalTable: "Terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Terminals_TerminalId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "GoodsReceivedNoteLines");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "Terminals");

            migrationBuilder.DropTable(
                name: "GoodsReceivedNotes");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Sales_IdempotencyKey",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TerminalId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "BalanceDueKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ChangeDueKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DiscountKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaidKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RoundingKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SubtotalKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TenderedKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TotalKobo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DiscountKobo",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "TotalKobo",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "UnitPriceKobo",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "AmountKobo",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "TerminalId",
                table: "Sales",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "IdempotencyKey",
                table: "Sales",
                newName: "Tendered");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceDue",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeDue",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Paid",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rounding",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
