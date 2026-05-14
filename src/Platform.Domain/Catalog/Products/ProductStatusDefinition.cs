namespace Platform.Domain.Catalog.Products;

public sealed class ProductStatusDefinition
{
    private ProductStatusDefinition()
    {
        Id = Guid.Empty;
        EntityType = ProductStatusEntityType.Product;
        Code = string.Empty;
        Name = string.Empty;
    }

    public ProductStatusDefinition(Guid id, ProductStatusEntityType entityType, string code, string name, bool isBuyable)
    {
        Id = id;
        EntityType = entityType;
        Code = code;
        Name = name;
        IsBuyable = isBuyable;
    }

    public Guid Id { get; private set; }
    public ProductStatusEntityType EntityType { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsBuyable { get; private set; }
}
