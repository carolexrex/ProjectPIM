using Platform.Contracts.Catalog.Attributes;

namespace Platform.Backoffice.Models;

public sealed class ProductAttributeListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Scope { get; init; }
    public string? DataType { get; init; }
    public IReadOnlyList<ProductAttributeSummaryDto> Attributes { get; init; } = [];
    public int Total { get; init; }
}
