using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropBonPreparationAddBlPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateEcheance",
                table: "BonsLivraison",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EstPayee",
                table: "BonsLivraison",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RemiseGlobale",
                table: "BonsLivraison",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTtc",
                table: "BonsLivraison",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE BonsLivraison
                SET DateEcheance = Date
                WHERE DateEcheance < '1900-01-01';
                """);

            migrationBuilder.CreateTable(
                name: "PaiementsBonLivraison",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonLivraisonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementsBonLivraison", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaiementsBonLivraison_BonsLivraison_BonLivraisonId",
                        column: x => x.BonLivraisonId,
                        principalTable: "BonsLivraison",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsBonLivraison_BonLivraisonId",
                table: "PaiementsBonLivraison",
                column: "BonLivraisonId");

            // Migrate BonPreparation → BonLivraison (headers, lines, payments, stock origins)
            // before dropping BP tables. Mapping uses insert order after capturing max BL Id.
            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE __MaxBl(Id INTEGER);
                INSERT INTO __MaxBl SELECT IFNULL(MAX(Id), 0) FROM BonsLivraison;

                INSERT INTO BonsLivraison (
                    Numero, ClientId, Date, DateEcheance, EstPayee, RemiseGlobale, TotalTtc, Note,
                    DevisId, BonCommandeClientId, FactureId, CreatedAt, UpdatedAt, CreatedByUserId)
                SELECT
                    CASE
                        WHEN EXISTS (SELECT 1 FROM BonsLivraison x WHERE x.Numero = bp.Numero)
                            THEN 'MIG-' || bp.Numero
                        ELSE bp.Numero
                    END,
                    bp.ClientId,
                    bp.Date,
                    bp.DateEcheance,
                    bp.EstPayee,
                    bp.RemiseGlobale,
                    bp.TotalTtc,
                    bp.Note,
                    NULL,
                    NULL,
                    NULL,
                    bp.CreatedAt,
                    bp.UpdatedAt,
                    bp.CreatedByUserId
                FROM BonsPreparation bp
                ORDER BY bp.Id;

                CREATE TEMP TABLE BpToBl AS
                SELECT bp.BpId AS BpId, bl.BlId AS BlId
                FROM (
                    SELECT Id AS BpId, ROW_NUMBER() OVER (ORDER BY Id) AS rn
                    FROM BonsPreparation
                ) bp
                INNER JOIN (
                    SELECT Id AS BlId, ROW_NUMBER() OVER (ORDER BY Id) AS rn
                    FROM BonsLivraison
                    WHERE Id > (SELECT Id FROM __MaxBl)
                ) bl ON bl.rn = bp.rn;

                INSERT INTO BonLivraisonLignes (
                    BLId, ProduitId, Designation, QuantiteCommandee, QuantiteLivree,
                    PrixUnitaireHT, Remise, TauxTVA, CreatedAt, UpdatedAt, CreatedByUserId)
                SELECT
                    m.BlId,
                    l.ProduitId,
                    l.Designation,
                    l.Quantite,
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA,
                    l.CreatedAt,
                    l.UpdatedAt,
                    l.CreatedByUserId
                FROM BonPreparationLignes l
                INNER JOIN BpToBl m ON m.BpId = l.BonPreparationId;

                INSERT INTO PaiementsBonLivraison (
                    BonLivraisonId, Montant, Date, Mode, Reference,
                    CreatedAt, UpdatedAt, CreatedByUserId)
                SELECT
                    m.BlId,
                    p.Montant,
                    p.Date,
                    p.Mode,
                    p.Reference,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.CreatedByUserId
                FROM PaiementsBonPreparation p
                INNER JOIN BpToBl m ON m.BpId = p.BonPreparationId;

                UPDATE MouvementsStock
                SET OrigineType = 'BL',
                    OrigineId = (
                        SELECT m.BlId FROM BpToBl m WHERE m.BpId = MouvementsStock.OrigineId)
                WHERE OrigineType = 'BP'
                  AND OrigineId IS NOT NULL
                  AND EXISTS (SELECT 1 FROM BpToBl m WHERE m.BpId = MouvementsStock.OrigineId);

                DROP TABLE IF EXISTS BpToBl;
                DROP TABLE IF EXISTS __MaxBl;
                """);

            migrationBuilder.DropTable(
                name: "BonPreparationLignes");

            migrationBuilder.DropTable(
                name: "PaiementsBonPreparation");

            migrationBuilder.DropTable(
                name: "BonsPreparation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaiementsBonLivraison");

            migrationBuilder.DropColumn(
                name: "DateEcheance",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "EstPayee",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "RemiseGlobale",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "TotalTtc",
                table: "BonsLivraison");

            migrationBuilder.CreateTable(
                name: "BonsPreparation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockLocationId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstPayee = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    RemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTtc = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsPreparation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonsPreparation_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonsPreparation_Tiers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonPreparationLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonPreparationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Conditionnement = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonPreparationLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonPreparationLignes_BonsPreparation_BonPreparationId",
                        column: x => x.BonPreparationId,
                        principalTable: "BonsPreparation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaiementsBonPreparation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonPreparationId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementsBonPreparation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaiementsBonPreparation_BonsPreparation_BonPreparationId",
                        column: x => x.BonPreparationId,
                        principalTable: "BonsPreparation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonPreparationLignes_BonPreparationId",
                table: "BonPreparationLignes",
                column: "BonPreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsPreparation_ClientId",
                table: "BonsPreparation",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsPreparation_StockLocationId",
                table: "BonsPreparation",
                column: "StockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsBonPreparation_BonPreparationId",
                table: "PaiementsBonPreparation",
                column: "BonPreparationId");
        }
    }
}
