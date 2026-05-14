namespace Platform.Domain.Catalog.Products;

public sealed class ProductAttributeValue
{
    private ProductAttributeValue()
    {
        Id = Guid.Empty;
    }

    public ProductAttributeValue(Guid id, Guid productAttributeId, Guid? attributeOptionId, string? valueText)
    {
        Id = id;
        ProductAttributeId = productAttributeId;
        AttributeOptionId = attributeOptionId;
        ValueText = valueText;
    }

    public Guid Id { get; private set; }
    public Guid ProductAttributeId { get; private set; }
    public Guid? AttributeOptionId { get; private set; }
    public string? ValueText { get; private set; }
}
