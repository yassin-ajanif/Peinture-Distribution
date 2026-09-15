namespace GestionCommerciale.Modules.Stock.Services;

public sealed record StockShortageItem(
    int ProduitId,
    string Reference,
    string Designation,
    string LocationName,
    decimal Demande,
    decimal Disponible,
    decimal Manquant);
