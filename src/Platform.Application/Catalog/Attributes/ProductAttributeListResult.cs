using Platform.Domain.Catalog.Attributes;

namespace Platform.Application.Catalog.Attributes;

public sealed record ProductAttributeListResult(IReadOnlyList<ProductAttribute> Items, int Total);
