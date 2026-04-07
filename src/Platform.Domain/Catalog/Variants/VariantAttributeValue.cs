namespace Platform.Domain.Catalog.Variants;

public sealed class VariantAttributeValue
{
    private VariantAttributeValue()
    {
        Id = Guid.Empty;
    }

    public VariantAttributeValue(Guid id, Guid productAttributeId, Guid? attributeOptionId, string? valueText)
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
