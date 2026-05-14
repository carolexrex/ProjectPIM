using Platform.Contracts.Catalog.Products;

namespace Platform.Backoffice.Models;

public sealed class ProductListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? ProductStatusCode { get; init; }
    public bool? HasVariants { get; init; }
    public string? Sort { get; init; }
    public IReadOnlyList<StatusOptionViewModel> ProductStatuses { get; init; } = [];
    public IReadOnlyList<ProductSummaryDto> Products { get; init; } = [];
    public int Total { get; init; }
    public int VisibleCount => Products.Count;
    public int ArchivedCount => Products.Count(product => string.Equals(product.Status, "Archived", StringComparison.OrdinalIgnoreCase));
    public int WithVariantsCount => Products.Count(product => product.HasVariants);
    public int BuyableCount => Products.Count(product => product.ProductStatus.IsBuyable);
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search)
        || !string.IsNullOrWhiteSpace(Status)
        || !string.IsNullOrWhiteSpace(ProductStatusCode)
        || HasVariants is not null
        || (!string.IsNullOrWhiteSpace(Sort)
            && !string.Equals(Sort, "productnumber", StringComparison.OrdinalIgnoreCase));
}
