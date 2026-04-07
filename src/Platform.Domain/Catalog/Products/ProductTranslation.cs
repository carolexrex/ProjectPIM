namespace Platform.Domain.Catalog.Products;

public sealed class ProductTranslation
{
    private ProductTranslation()
    {
        Id = Guid.Empty;
        CultureCode = string.Empty;
        Name = string.Empty;
    }

    public ProductTranslation(
        Guid id,
        string cultureCode,
        string name,
        string? shortDescription,
        string? longDescription,
        string? seoTitle,
        string? seoDescription)
    {
        Id = id;
        CultureCode = cultureCode;
        Name = name;
        ShortDescription = shortDescription;
        LongDescription = longDescription;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
    }

    public Guid Id { get; private set; }
    public string CultureCode { get; private set; }
    public string Name { get; private set; }
    public string? ShortDescription { get; private set; }
    public string? LongDescription { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }

    public void Update(
        string name,
        string? shortDescription,
        string? longDescription,
        string? seoTitle,
        string? seoDescription)
    {
        Name = name;
        ShortDescription = shortDescription;
        LongDescription = longDescription;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
    }
}
