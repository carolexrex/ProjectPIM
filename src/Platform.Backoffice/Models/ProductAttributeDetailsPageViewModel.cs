using Platform.Contracts.Catalog.Attributes;

namespace Platform.Backoffice.Models;

public sealed class ProductAttributeDetailsPageViewModel
{
    public ProductAttributeUpdateViewModel Attribute { get; init; } = new();
    public IReadOnlyList<AttributeOptionDto> Options { get; init; } = [];
}
