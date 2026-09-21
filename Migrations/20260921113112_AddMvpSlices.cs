using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenRetail.Migrations
{
    /// <inheritdoc />
    public partial class AddMvpSlices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Users_UserId",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_Users_CashierId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_CashierId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_Products_ProductId",
                table: "StockLedger");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Products_ProductId",
                table: "StockLevels");

            migrationBuilder.RenameColumn(
                name: "RequiresPinChange",
                table: "Users",
                newName: "RequiresPasswordChange");

            migrationBuilder.RenameColumn(
                name: "PinSalt",
                table: "Users",
                newName: "PasswordSalt");

            migrationBuilder.RenameColumn(
                name: "PinHash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Perishable",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RefundAllowed",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresBatch",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresExpiry",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSecurityReturnCheck",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Module = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VerifiedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptVerifications_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptVerifications_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReportedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    IncidentType = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityIncidents_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityIncidents_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentRequests_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentRequests_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockOverrideRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolvedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockOverrideRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockOverrideRequests_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockOverrideRequests_Users_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockOverrideRequests_Users_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreCreditVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    AmountKobo = table.Column<long>(type: "INTEGER", nullable: false),
                    BalanceKobo = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiryUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SourceSaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceRefundId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerPhone = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreCreditVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreCreditVouchers_Sales_SourceSaleId",
                        column: x => x.SourceSaleId,
                        principalTable: "Sales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GrantedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionOverrides_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionOverrides_Users_GrantedById",
                        column: x => x.GrantedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionOverrides_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssignedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CashierManagerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    TotalKobo = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreCreditVoucherId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundRequests_StoreCreditVouchers_StoreCreditVoucherId",
                        column: x => x.StoreCreditVoucherId,
                        principalTable: "StoreCreditVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Users_CashierManagerId",
                        column: x => x.CashierManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreCreditRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VoucherId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AmountKobo = table.Column<long>(type: "INTEGER", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RedeemedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreCreditRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreCreditRedemptions_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreCreditRedemptions_StoreCreditVouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "StoreCreditVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionOverrides_GrantedById",
                table: "PermissionOverrides",
                column: "GrantedById");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionOverrides_PermissionId",
                table: "PermissionOverrides",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionOverrides_UserId",
                table: "PermissionOverrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVerifications_SaleId",
                table: "ReceiptVerifications",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVerifications_VerifiedById",
                table: "ReceiptVerifications",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_CashierManagerId",
                table: "RefundRequests",
                column: "CashierManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_SaleId",
                table: "RefundRequests",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_Status",
                table: "RefundRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_StoreCreditVoucherId",
                table: "RefundRequests",
                column: "StoreCreditVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_CreatedUtc",
                table: "SecurityIncidents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_ReportedById",
                table: "SecurityIncidents",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_SaleId",
                table: "SecurityIncidents",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentRequests_ApprovedById",
                table: "StockAdjustmentRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentRequests_ProductId",
                table: "StockAdjustmentRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentRequests_RequestedById",
                table: "StockAdjustmentRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentRequests_Status",
                table: "StockAdjustmentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockOverrideRequests_CashierId",
                table: "StockOverrideRequests",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOverrideRequests_ProductId",
                table: "StockOverrideRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOverrideRequests_ResolvedById",
                table: "StockOverrideRequests",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockOverrideRequests_Status",
                table: "StockOverrideRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCreditRedemptions_SaleId",
                table: "StoreCreditRedemptions",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCreditRedemptions_VoucherId",
                table: "StoreCreditRedemptions",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCreditVouchers_Code",
                table: "StoreCreditVouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreCreditVouchers_SourceSaleId",
                table: "StoreCreditVouchers",
                column: "SourceSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_BranchId",
                table: "UserRoles",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Users_UserId",
                table: "AuditLog",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_Users_CashierId",
                table: "CashSessions",
                column: "CashierId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_CashierId",
                table: "Sales",
                column: "CashierId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_Products_ProductId",
                table: "StockLedger",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Products_ProductId",
                table: "StockLevels",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Users_UserId",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_Users_CashierId",
                table: "CashSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_CashierId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_Products_ProductId",
                table: "StockLedger");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLevels_Products_ProductId",
                table: "StockLevels");

            migrationBuilder.DropTable(
                name: "PermissionOverrides");

            migrationBuilder.DropTable(
                name: "ReceiptVerifications");

            migrationBuilder.DropTable(
                name: "RefundRequests");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SecurityIncidents");

            migrationBuilder.DropTable(
                name: "StockAdjustmentRequests");

            migrationBuilder.DropTable(
                name: "StockOverrideRequests");

            migrationBuilder.DropTable(
                name: "StoreCreditRedemptions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "StoreCreditVouchers");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Perishable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RefundAllowed",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresBatch",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresExpiry",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresSecurityReturnCheck",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "RequiresPasswordChange",
                table: "Users",
                newName: "RequiresPinChange");

            migrationBuilder.RenameColumn(
                name: "PasswordSalt",
                table: "Users",
                newName: "PinSalt");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Users",
                newName: "PinHash");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Users_UserId",
                table: "AuditLog",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_Users_CashierId",
                table: "CashSessions",
                column: "CashierId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_CashierId",
                table: "Sales",
                column: "CashierId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_Products_ProductId",
                table: "StockLedger",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLevels_Products_ProductId",
                table: "StockLevels",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
