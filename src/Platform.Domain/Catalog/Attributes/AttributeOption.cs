namespace Platform.Domain.Catalog.Attributes;

public sealed class AttributeOption
{
    private AttributeOption()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Value = string.Empty;
    }

    public AttributeOption(Guid id, string code, string value, int sortOrder)
    {
        Id = id;
        Code = code;
        Value = value;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Value { get; private set; }
    public int SortOrder { get; private set; }
}
