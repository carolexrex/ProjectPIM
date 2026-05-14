using Platform.Domain.Catalog.Brands;

namespace Platform.Application.Catalog.Brands;

public sealed record BrandListResult(
    IReadOnlyList<Brand> Items,
    int Total);
