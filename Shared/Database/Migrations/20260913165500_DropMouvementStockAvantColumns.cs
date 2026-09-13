using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GestionCommerciale.Shared.Database;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260913165500_DropMouvementStockAvantColumns")]
public partial class DropMouvementStockAvantColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FromAvant",
            table: "MouvementsStock");

        migrationBuilder.DropColumn(
            name: "ToAvant",
            table: "MouvementsStock");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "FromAvant",
            table: "MouvementsStock",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ToAvant",
            table: "MouvementsStock",
            type: "TEXT",
            nullable: true);
    }
}
