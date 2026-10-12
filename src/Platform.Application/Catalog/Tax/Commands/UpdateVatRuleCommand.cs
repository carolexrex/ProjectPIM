namespace Platform.Application.Catalog.Tax.Commands;

public sealed record UpdateVatRuleCommand(
    Guid VatRuleId,
    decimal Rate,
    string? Description,
    string RowVersion);
