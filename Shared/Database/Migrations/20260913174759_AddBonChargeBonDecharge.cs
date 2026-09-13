using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBonChargeBonDecharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonsCharge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedToUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepotLocationId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsCharge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonsCharge_StockLocations_DepotLocationId",
                        column: x => x.DepotLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonsCharge_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonsDecharge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedToUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepotLocationId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsDecharge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonsDecharge_StockLocations_DepotLocationId",
                        column: x => x.DepotLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonsDecharge_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonChargeLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonChargeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonChargeLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonChargeLignes_BonsCharge_BonChargeId",
                        column: x => x.BonChargeId,
                        principalTable: "BonsCharge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonChargeLignes_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonDechargeLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonDechargeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonDechargeLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonDechargeLignes_BonsDecharge_BonDechargeId",
                        column: x => x.BonDechargeId,
                        principalTable: "BonsDecharge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonDechargeLignes_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonChargeLignes_BonChargeId",
                table: "BonChargeLignes",
                column: "BonChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_BonChargeLignes_ProduitId",
                table: "BonChargeLignes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_BonDechargeLignes_BonDechargeId",
                table: "BonDechargeLignes",
                column: "BonDechargeId");

            migrationBuilder.CreateIndex(
                name: "IX_BonDechargeLignes_ProduitId",
                table: "BonDechargeLignes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCharge_AssignedToUserId",
                table: "BonsCharge",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCharge_DepotLocationId",
                table: "BonsCharge",
                column: "DepotLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCharge_Numero",
                table: "BonsCharge",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonsDecharge_AssignedToUserId",
                table: "BonsDecharge",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsDecharge_DepotLocationId",
                table: "BonsDecharge",
                column: "DepotLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsDecharge_Numero",
                table: "BonsDecharge",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonChargeLignes");

            migrationBuilder.DropTable(
                name: "BonDechargeLignes");

            migrationBuilder.DropTable(
                name: "BonsCharge");

            migrationBuilder.DropTable(
                name: "BonsDecharge");
        }
    }
}
