using GestionCommerciale.Modules.Facturation.Models;

namespace GestionCommerciale.Modules.Facturation.Services;

public sealed record BulkPayableDocument(
    int DocumentId,
    string Numero,
    DateTime Date,
    decimal TotalTtc,
    decimal AlreadyPaid,
    decimal Remaining);

public sealed record BulkPaymentAllocationLine(
    int DocumentId,
    string Numero,
    DateTime DocumentDate,
    decimal Amount,
    decimal RemainingAfter,
    bool WillBeFullyPaid);

public sealed record BulkPaymentPreview(
    decimal RequestedAmount,
    decimal TotalRemaining,
    IReadOnlyList<BulkPaymentAllocationLine> Lines);

public sealed record ClientBulkPaymentRequest(
    int ClientId,
    decimal Amount,
    DateTime Date,
    ModePaiement Mode,
    string Reference);
