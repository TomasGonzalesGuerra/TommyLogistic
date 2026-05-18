using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TommyLogistic.API.Migrations
{
    /// <inheritdoc />
    public partial class AdsCargaEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CargaID",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cargas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaConcluida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFacturada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NotaConclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotaFacturacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SupervisorID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcluidaPorID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FacturadaPorID = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cargas_AspNetUsers_ConcluidaPorID",
                        column: x => x.ConcluidaPorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargas_AspNetUsers_FacturadaPorID",
                        column: x => x.FacturadaPorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargas_AspNetUsers_SupervisorID",
                        column: x => x.SupervisorID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cargas_Drivers_DriverID",
                        column: x => x.DriverID,
                        principalTable: "Drivers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CargaID",
                table: "Orders",
                column: "CargaID");

            migrationBuilder.CreateIndex(
                name: "IX_Cargas_ConcluidaPorID",
                table: "Cargas",
                column: "ConcluidaPorID");

            migrationBuilder.CreateIndex(
                name: "IX_Cargas_DriverID",
                table: "Cargas",
                column: "DriverID");

            migrationBuilder.CreateIndex(
                name: "IX_Cargas_FacturadaPorID",
                table: "Cargas",
                column: "FacturadaPorID");

            migrationBuilder.CreateIndex(
                name: "IX_Cargas_SupervisorID",
                table: "Cargas",
                column: "SupervisorID");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Cargas_CargaID",
                table: "Orders",
                column: "CargaID",
                principalTable: "Cargas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Cargas_CargaID",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Cargas");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CargaID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CargaID",
                table: "Orders");
        }
    }
}
