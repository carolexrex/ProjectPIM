using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Products;

public sealed class Product
{
    private readonly List<ProductTranslation> _translations = [];

    private Product()
    {
        Id = Guid.Empty;
        ProductType = string.Empty;
        ProductNumber = string.Empty;
        Slug = string.Empty;
        ProductStatus = new ProductStatusDefinition(Guid.Empty, string.Empty, string.Empty, false);
        TaxCategoryCode = string.Empty;
        UnitOfMeasure = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Product(
        Guid id,
        string productType,
        string productNumber,
        string slug,
        Guid? brandId,
        ProductStatusDefinition productStatus,
        string taxCategoryCode,
        string unitOfMeasure,
        bool hasVariants,
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        ProductType = productType;
        ProductNumber = productNumber;
        Slug = slug;
        BrandId = brandId;
        ProductStatus = productStatus;
        TaxCategoryCode = taxCategoryCode;
        UnitOfMeasure = unitOfMeasure;
        HasVariants = hasVariants;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Status = "Active";
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string ProductType { get; private set; }
    public string ProductNumber { get; private set; }
    public string Slug { get; private set; }
    public Guid? BrandId { get; private set; }
    public ProductStatusDefinition ProductStatus { get; private set; }
    public string TaxCategoryCode { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public bool HasVariants { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public string Status { get; private set; }
    public string? PrimaryImageUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<ProductTranslation> Translations => _translations;

    public void Update(
        string productType,
        string slug,
        Guid? brandId,
        ProductStatusDefinition productStatus,
        string taxCategoryCode,
        string unitOfMeasure,
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        ProductType = productType;
        Slug = slug;
        BrandId = brandId;
        ProductStatus = productStatus;
        TaxCategoryCode = taxCategoryCode;
        UnitOfMeasure = unitOfMeasure;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public void AssignStatus(ProductStatusDefinition productStatus)
    {
        ProductStatus = productStatus;
        Touch();
    }

    public ProductTranslation UpsertTranslation(
        string cultureCode,
        string name,
        string? shortDescription,
        string? longDescription,
        string? seoTitle,
        string? seoDescription)
    {
        var existing = _translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Update(name, shortDescription, longDescription, seoTitle, seoDescription);
            Touch();
            return existing;
        }

        var translation = new ProductTranslation(Guid.NewGuid(), cultureCode, name, shortDescription, longDescription, seoTitle, seoDescription);
        _translations.Add(translation);
        Touch();
        return translation;
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The product has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
