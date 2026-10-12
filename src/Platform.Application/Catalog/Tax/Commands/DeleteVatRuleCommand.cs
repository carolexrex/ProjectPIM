namespace Platform.Application.Catalog.Tax.Commands;

public sealed record DeleteVatRuleCommand(
    Guid VatRuleId,
    string RowVersion);
