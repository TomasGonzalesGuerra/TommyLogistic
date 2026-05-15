using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TommyLogistic.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderEventFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAttempt",
                table: "OrderEvents");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "OrderEvents");

            migrationBuilder.DropColumn(
                name: "RescheduledFor",
                table: "OrderEvents");

            migrationBuilder.RenameColumn(
                name: "StatusAfter",
                table: "OrderEvents",
                newName: "NewStatus");

            migrationBuilder.RenameColumn(
                name: "OccurredAt",
                table: "OrderEvents",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "OrderEvents",
                newName: "Note");

            migrationBuilder.AlterColumn<string>(
                name: "BaglokLocation",
                table: "OrderEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AssignedDriverID",
                table: "OrderEvents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_AssignedDriverID",
                table: "OrderEvents",
                column: "AssignedDriverID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderEvents_Drivers_AssignedDriverID",
                table: "OrderEvents",
                column: "AssignedDriverID",
                principalTable: "Drivers",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderEvents_Drivers_AssignedDriverID",
                table: "OrderEvents");

            migrationBuilder.DropIndex(
                name: "IX_OrderEvents_AssignedDriverID",
                table: "OrderEvents");

            migrationBuilder.DropColumn(
                name: "AssignedDriverID",
                table: "OrderEvents");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "OrderEvents",
                newName: "OccurredAt");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "OrderEvents",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "NewStatus",
                table: "OrderEvents",
                newName: "StatusAfter");

            migrationBuilder.AlterColumn<string>(
                name: "BaglokLocation",
                table: "OrderEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempt",
                table: "OrderEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EventType",
                table: "OrderEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduledFor",
                table: "OrderEvents",
                type: "datetime2",
                nullable: true);
        }
    }
}
