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
        // EF's SQLite DropColumn rebuilds the whole table and can hang forever on this DB.
        // Native DROP COLUMN is supported by SQLite 3.35+ (bundled with Microsoft.Data.Sqlite).
        migrationBuilder.Sql("""
            ALTER TABLE MouvementsStock DROP COLUMN FromAvant;
            ALTER TABLE MouvementsStock DROP COLUMN ToAvant;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE MouvementsStock ADD COLUMN FromAvant TEXT NULL;
            ALTER TABLE MouvementsStock ADD COLUMN ToAvant TEXT NULL;
            """);
    }
}
