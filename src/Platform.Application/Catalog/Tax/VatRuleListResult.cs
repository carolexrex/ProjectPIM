using Platform.Domain.Catalog.Tax;

namespace Platform.Application.Catalog.Tax;

public sealed record VatRuleListResult(
    IReadOnlyList<VatRule> Items,
    int Total);
