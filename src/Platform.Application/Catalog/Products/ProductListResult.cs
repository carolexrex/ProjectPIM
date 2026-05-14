using Platform.Domain.Catalog.Products;

namespace Platform.Application.Catalog.Products;

public sealed record ProductListResult(
    IReadOnlyList<Product> Items,
    int Total);
