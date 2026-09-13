using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersStockLocationsDropStockActuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Vendeur"),
                    Actif = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsVirtual = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Actif = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLocations", x => x.Id);
                    table.CheckConstraint("CK_StockLocation_VirtualUser", "(IsVirtual = 0 AND UserId IS NULL) OR (IsVirtual = 1 AND UserId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_StockLocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "StockLocations",
                columns: new[] { "Id", "Actif", "CreatedAt", "CreatedByUserId", "IsVirtual", "Nom", "UpdatedAt", "UserId" },
                values: new object[] { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "Dépôt principal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.AddColumn<decimal>(
                name: "FromApres",
                table: "MouvementsStock",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FromAvant",
                table: "MouvementsStock",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FromLocationId",
                table: "MouvementsStock",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ToApres",
                table: "MouvementsStock",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ToAvant",
                table: "MouvementsStock",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToLocationId",
                table: "MouvementsStock",
                type: "INTEGER",
                nullable: true);

            // Entree (0) or positive Ajustement (2): into dépôt
            migrationBuilder.Sql("""
                UPDATE MouvementsStock
                SET FromLocationId = NULL,
                    ToLocationId = 1,
                    FromAvant = NULL,
                    FromApres = NULL,
                    ToAvant = StockAvant,
                    ToApres = StockAvant + ABS(Quantite),
                    Quantite = ABS(Quantite)
                WHERE Type = 0 OR (Type = 2 AND Quantite >= 0);
                """);

            // Sortie (1) or negative Ajustement (2): out of dépôt
            migrationBuilder.Sql("""
                UPDATE MouvementsStock
                SET FromLocationId = 1,
                    ToLocationId = NULL,
                    FromAvant = StockAvant,
                    FromApres = StockAvant - ABS(Quantite),
                    ToAvant = NULL,
                    ToApres = NULL,
                    Quantite = ABS(Quantite)
                WHERE Type = 1 OR (Type = 2 AND Quantite < 0);
                """);

            // Opening balance for products with StockActuel but no movements
            migrationBuilder.Sql("""
                INSERT INTO MouvementsStock (
                    CreatedAt, UpdatedAt, CreatedByUserId, ProduitId,
                    FromLocationId, ToLocationId, Quantite,
                    FromAvant, FromApres, ToAvant, ToApres,
                    OrigineType, OrigineId, Note, Type, StockAvant)
                SELECT
                    datetime('now'), datetime('now'), NULL, p.Id,
                    NULL, 1, p.StockActuel,
                    NULL, NULL, 0, p.StockActuel,
                    'Migration', NULL, 'Solde initial', 0, 0
                FROM Produits p
                WHERE p.StockActuel > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM MouvementsStock m WHERE m.ProduitId = p.Id);
                """);

            migrationBuilder.DropColumn(
                name: "StockAvant",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "StockActuel",
                table: "Produits");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_FromLocationId",
                table: "MouvementsStock",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_OrigineType_OrigineId",
                table: "MouvementsStock",
                columns: new[] { "OrigineType", "OrigineId" });

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_ToLocationId",
                table: "MouvementsStock",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_UserId",
                table: "StockLocations",
                column: "UserId",
                unique: true,
                filter: "IsVirtual = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MouvementsStock_StockLocations_FromLocationId",
                table: "MouvementsStock",
                column: "FromLocationId",
                principalTable: "StockLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MouvementsStock_StockLocations_ToLocationId",
                table: "MouvementsStock",
                column: "ToLocationId",
                principalTable: "StockLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MouvementsStock_StockLocations_FromLocationId",
                table: "MouvementsStock");

            migrationBuilder.DropForeignKey(
                name: "FK_MouvementsStock_StockLocations_ToLocationId",
                table: "MouvementsStock");

            migrationBuilder.DropTable(
                name: "StockLocations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_FromLocationId",
                table: "MouvementsStock");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_OrigineType_OrigineId",
                table: "MouvementsStock");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_ToLocationId",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "FromApres",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "FromAvant",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "FromLocationId",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "ToApres",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "ToAvant",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "ToLocationId",
                table: "MouvementsStock");

            migrationBuilder.AddColumn<decimal>(
                name: "StockActuel",
                table: "Produits",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockAvant",
                table: "MouvementsStock",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "MouvementsStock",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
