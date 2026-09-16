using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBonLivraisonVendeurId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendeurId",
                table: "BonsLivraison",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_VendeurId",
                table: "BonsLivraison",
                column: "VendeurId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_Users_VendeurId",
                table: "BonsLivraison",
                column: "VendeurId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_Users_VendeurId",
                table: "BonsLivraison");

            migrationBuilder.DropIndex(
                name: "IX_BonsLivraison_VendeurId",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "VendeurId",
                table: "BonsLivraison");
        }
    }
}
