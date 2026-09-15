using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTiersForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove orphan party ids (0 / missing Tiers) before enforcing FKs.
            migrationBuilder.Sql("""
                DELETE FROM MouvementsStock
                WHERE (OrigineType = 'Avoir' AND OrigineId IN (
                    SELECT Id FROM Avoirs WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers)))
                   OR (OrigineType = 'AvoirFournisseur' AND OrigineId IN (
                    SELECT Id FROM AvoirsFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers)))
                   OR (OrigineType = 'BL' AND OrigineId IN (
                    SELECT Id FROM BonsLivraison WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers)))
                   OR (OrigineType = 'BP' AND OrigineId IN (
                    SELECT Id FROM BonsPreparation WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers)))
                   OR (OrigineType = 'BR' AND OrigineId IN (
                    SELECT Id FROM BonsReception WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers)));

                DELETE FROM AvoirLignes WHERE AvoirId IN (
                    SELECT Id FROM Avoirs WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM Avoirs WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);

                DELETE FROM AvoirFournisseurLignes WHERE AvoirFournisseurId IN (
                    SELECT Id FROM AvoirsFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM AvoirsFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers);

                DELETE FROM DevisLignes WHERE DevisId IN (
                    SELECT Id FROM Devis WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM Devis WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);

                UPDATE BonsLivraison SET FactureId = NULL WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);
                UPDATE FactureLignes SET BonLivraisonId = NULL WHERE BonLivraisonId IN (
                    SELECT Id FROM BonsLivraison WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonLivraisonLignes WHERE BLId IN (
                    SELECT Id FROM BonsLivraison WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonsLivraison WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);

                UPDATE BonsCommandeClient SET FactureId = NULL WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);
                UPDATE BonsLivraison SET BonCommandeClientId = NULL WHERE BonCommandeClientId IN (
                    SELECT Id FROM BonsCommandeClient WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonCommandeClientLignes WHERE BonCommandeClientId IN (
                    SELECT Id FROM BonsCommandeClient WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonsCommandeClient WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);

                DELETE FROM Paiements WHERE FactureId IN (
                    SELECT Id FROM Factures WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM FactureLignes WHERE FactureId IN (
                    SELECT Id FROM Factures WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                UPDATE Avoirs SET FactureId = NULL WHERE FactureId IN (
                    SELECT Id FROM Factures WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                UPDATE BonsLivraison SET FactureId = NULL WHERE FactureId IN (
                    SELECT Id FROM Factures WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                UPDATE BonsCommandeClient SET FactureId = NULL WHERE FactureId IN (
                    SELECT Id FROM Factures WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM Factures WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);

                DELETE FROM PaiementsBonPreparation WHERE BonPreparationId IN (
                    SELECT Id FROM BonsPreparation WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonPreparationLignes WHERE BonPreparationId IN (
                    SELECT Id FROM BonsPreparation WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonsPreparation WHERE ClientId <= 0 OR ClientId NOT IN (SELECT Id FROM Tiers);

                DELETE FROM BonCommandeLignes WHERE BonCommandeId IN (
                    SELECT Id FROM BonsCommande WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                UPDATE BonsReception SET BonCommandeId = NULL WHERE BonCommandeId IN (
                    SELECT Id FROM BonsCommande WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonsCommande WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers);

                UPDATE FactureFournisseurLignes SET BonReceptionId = NULL WHERE BonReceptionId IN (
                    SELECT Id FROM BonsReception WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                UPDATE BonsReception SET FactureFournisseurId = NULL WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers);
                DELETE FROM BonReceptionLignes WHERE BRId IN (
                    SELECT Id FROM BonsReception WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM BonsReception WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers);

                DELETE FROM PaiementsFournisseurs WHERE FactureFournisseurId IN (
                    SELECT Id FROM FacturesFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM FactureFournisseurLignes WHERE FactureFournisseurId IN (
                    SELECT Id FROM FacturesFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                UPDATE BonsReception SET FactureFournisseurId = NULL WHERE FactureFournisseurId IN (
                    SELECT Id FROM FacturesFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers));
                DELETE FROM FacturesFournisseurs WHERE FournisseurId <= 0 OR FournisseurId NOT IN (SELECT Id FROM Tiers);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FacturesFournisseurs_FournisseurId",
                table: "FacturesFournisseurs",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ClientId",
                table: "Factures",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_ClientId",
                table: "Devis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsReception_FournisseurId",
                table: "BonsReception",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsPreparation_ClientId",
                table: "BonsPreparation",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_ClientId",
                table: "BonsLivraison",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCommandeClient_ClientId",
                table: "BonsCommandeClient",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCommande_FournisseurId",
                table: "BonsCommande",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_AvoirsFournisseurs_FournisseurId",
                table: "AvoirsFournisseurs",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_Avoirs_ClientId",
                table: "Avoirs",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avoirs_Tiers_ClientId",
                table: "Avoirs",
                column: "ClientId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvoirsFournisseurs_Tiers_FournisseurId",
                table: "AvoirsFournisseurs",
                column: "FournisseurId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsCommande_Tiers_FournisseurId",
                table: "BonsCommande",
                column: "FournisseurId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsCommandeClient_Tiers_ClientId",
                table: "BonsCommandeClient",
                column: "ClientId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_Tiers_ClientId",
                table: "BonsLivraison",
                column: "ClientId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsPreparation_Tiers_ClientId",
                table: "BonsPreparation",
                column: "ClientId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsReception_Tiers_FournisseurId",
                table: "BonsReception",
                column: "FournisseurId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devis_Tiers_ClientId",
                table: "Devis",
                column: "ClientId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Factures_Tiers_ClientId",
                table: "Factures",
                column: "ClientId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturesFournisseurs_Tiers_FournisseurId",
                table: "FacturesFournisseurs",
                column: "FournisseurId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avoirs_Tiers_ClientId",
                table: "Avoirs");

            migrationBuilder.DropForeignKey(
                name: "FK_AvoirsFournisseurs_Tiers_FournisseurId",
                table: "AvoirsFournisseurs");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsCommande_Tiers_FournisseurId",
                table: "BonsCommande");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsCommandeClient_Tiers_ClientId",
                table: "BonsCommandeClient");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_Tiers_ClientId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsPreparation_Tiers_ClientId",
                table: "BonsPreparation");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsReception_Tiers_FournisseurId",
                table: "BonsReception");

            migrationBuilder.DropForeignKey(
                name: "FK_Devis_Tiers_ClientId",
                table: "Devis");

            migrationBuilder.DropForeignKey(
                name: "FK_Factures_Tiers_ClientId",
                table: "Factures");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturesFournisseurs_Tiers_FournisseurId",
                table: "FacturesFournisseurs");

            migrationBuilder.DropIndex(
                name: "IX_FacturesFournisseurs_FournisseurId",
                table: "FacturesFournisseurs");

            migrationBuilder.DropIndex(
                name: "IX_Factures_ClientId",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Devis_ClientId",
                table: "Devis");

            migrationBuilder.DropIndex(
                name: "IX_BonsReception_FournisseurId",
                table: "BonsReception");

            migrationBuilder.DropIndex(
                name: "IX_BonsPreparation_ClientId",
                table: "BonsPreparation");

            migrationBuilder.DropIndex(
                name: "IX_BonsLivraison_ClientId",
                table: "BonsLivraison");

            migrationBuilder.DropIndex(
                name: "IX_BonsCommandeClient_ClientId",
                table: "BonsCommandeClient");

            migrationBuilder.DropIndex(
                name: "IX_BonsCommande_FournisseurId",
                table: "BonsCommande");

            migrationBuilder.DropIndex(
                name: "IX_AvoirsFournisseurs_FournisseurId",
                table: "AvoirsFournisseurs");

            migrationBuilder.DropIndex(
                name: "IX_Avoirs_ClientId",
                table: "Avoirs");
        }
    }
}
