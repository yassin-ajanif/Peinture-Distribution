namespace GestionCommerciale.Modules.Stock.ViewModels;

public sealed class StockLocationPickItem
{
    public int Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsVirtual { get; init; }

    public override string ToString() => Label;
}
