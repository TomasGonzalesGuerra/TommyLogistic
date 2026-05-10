using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TommyLogistic.API.Migrations
{
    /// <inheritdoc />
    public partial class QuePutada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    StatusAfter = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaglokLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RescheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryAttempt = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderEvents_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderEvents_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_OrderID",
                table: "OrderEvents",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_UserID",
                table: "OrderEvents",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderEvents");
        }
    }
}
