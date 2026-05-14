namespace Platform.Domain.Catalog.Products;

public sealed class ProductRelation
{
    private ProductRelation()
    {
        Id = Guid.Empty;
        TargetProductId = Guid.Empty;
        RelationType = string.Empty;
    }

    public ProductRelation(Guid id, Guid targetProductId, string relationType, decimal? quantity, int sortOrder)
    {
        Id = id;
        TargetProductId = targetProductId;
        RelationType = relationType;
        Quantity = quantity;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid TargetProductId { get; private set; }
    public string RelationType { get; private set; }
    public decimal? Quantity { get; private set; }
    public int SortOrder { get; private set; }

    public void Update(decimal? quantity, int sortOrder)
    {
        Quantity = quantity;
        SortOrder = sortOrder;
    }
}
