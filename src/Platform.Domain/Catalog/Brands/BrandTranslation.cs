namespace Platform.Domain.Catalog.Brands;

public sealed class BrandTranslation
{
    private BrandTranslation()
    {
        Id = Guid.Empty;
        CultureCode = string.Empty;
        Name = string.Empty;
        Slug = string.Empty;
    }

    public BrandTranslation(Guid id, string cultureCode, string name, string slug, string? description)
    {
        Id = id;
        CultureCode = cultureCode;
        Name = name;
        Slug = slug;
        Description = description;
    }

    public Guid Id { get; private set; }
    public string CultureCode { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }

    public void Update(string name, string slug, string? description)
    {
        Name = name;
        Slug = slug;
        Description = description;
    }
}
