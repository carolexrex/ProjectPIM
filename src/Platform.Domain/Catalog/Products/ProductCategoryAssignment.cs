namespace Platform.Domain.Catalog.Products;

public sealed class ProductCategoryAssignment
{
    private ProductCategoryAssignment()
    {
        Id = Guid.Empty;
        CategoryId = Guid.Empty;
    }

    public ProductCategoryAssignment(Guid id, Guid categoryId)
    {
        Id = id;
        CategoryId = categoryId;
    }

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
}
