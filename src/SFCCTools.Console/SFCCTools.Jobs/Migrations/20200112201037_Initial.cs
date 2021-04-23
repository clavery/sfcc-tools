using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SFCCTools.Jobs.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<string>(nullable: false),
                    CustomerNo = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    RemoteHost = table.Column<IPAddress>(nullable: true),
                    TaxTotal = table.Column<decimal>(nullable: false),
                    ShippingTotal = table.Column<decimal>(nullable: false),
                    ProductTotal = table.Column<decimal>(nullable: false),
                    OrderTotal = table.Column<decimal>(nullable: false),
                    BillingStateCode = table.Column<string>(nullable: true),
                    ShippingStateCode = table.Column<string>(nullable: true),
                    BillingCountryCode = table.Column<string>(nullable: true),
                    ShippingCountryCode = table.Column<string>(nullable: true),
                    ShippingMethod = table.Column<string>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeConfigs",
                columns: table => new
                {
                    RuntimeConfigId = table.Column<string>(nullable: false),
                    DateConfig = table.Column<DateTime>(nullable: false),
                    StringConfig = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeConfigs", x => x.RuntimeConfigId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                columns: table => new
                {
                    Index = table.Column<int>(nullable: false),
                    OrderId = table.Column<string>(nullable: false),
                    Method = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod", x => new { x.Index, x.OrderId });
                    table.ForeignKey(
                        name: "FK_PaymentMethod_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductLineItem",
                columns: table => new
                {
                    Index = table.Column<int>(nullable: false),
                    OrderId = table.Column<string>(nullable: false),
                    ProductId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLineItem", x => new { x.Index, x.OrderId });
                    table.ForeignKey(
                        name: "FK_ProductLineItem_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BillingCountryCode",
                table: "Orders",
                column: "BillingCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BillingStateCode",
                table: "Orders",
                column: "BillingStateCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreationDate",
                table: "Orders",
                column: "CreationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerNo",
                table: "Orders",
                column: "CustomerNo");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RemoteHost",
                table: "Orders",
                column: "RemoteHost");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingCountryCode",
                table: "Orders",
                column: "ShippingCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingMethod",
                table: "Orders",
                column: "ShippingMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingStateCode",
                table: "Orders",
                column: "ShippingStateCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_Method",
                table: "PaymentMethod",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_OrderId",
                table: "PaymentMethod",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLineItem_OrderId",
                table: "ProductLineItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLineItem_ProductId",
                table: "ProductLineItem",
                column: "ProductId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethod");

            migrationBuilder.DropTable(
                name: "ProductLineItem");

            migrationBuilder.DropTable(
                name: "RuntimeConfigs");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
