using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBonPreparationStockLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockLocationId",
                table: "BonsPreparation",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_BonsPreparation_StockLocationId",
                table: "BonsPreparation",
                column: "StockLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsPreparation_StockLocations_StockLocationId",
                table: "BonsPreparation",
                column: "StockLocationId",
                principalTable: "StockLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsPreparation_StockLocations_StockLocationId",
                table: "BonsPreparation");

            migrationBuilder.DropIndex(
                name: "IX_BonsPreparation_StockLocationId",
                table: "BonsPreparation");

            migrationBuilder.DropColumn(
                name: "StockLocationId",
                table: "BonsPreparation");
        }
    }
}
