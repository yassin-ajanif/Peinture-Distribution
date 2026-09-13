using GestionCommerciale.Modules.Personnel.Models;

namespace GestionCommerciale.Modules.Personnel.ViewModels;

public sealed class BonDechargeListRow
{
    public BonDecharge Doc { get; init; } = null!;
    public string AssignedToNom { get; init; } = string.Empty;
    public string DepotNom { get; init; } = string.Empty;
    public string DateShort => Doc.Date.ToString("dd/MM/yyyy");
    public string NotePreview => string.IsNullOrWhiteSpace(Doc.Note)
        ? string.Empty
        : (Doc.Note.Length <= 40 ? Doc.Note : Doc.Note[..40] + "…");
}
