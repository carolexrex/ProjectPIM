using Platform.Domain.Common;

using Platform.Domain.Catalog.Products;

namespace Platform.Domain.Catalog.Variants;

public sealed class Variant
{
    private readonly List<VariantAttributeValue> _attributeValues = [];

    private Variant()
    {
        Id = Guid.Empty;
        ProductId = Guid.Empty;
        Sku = string.Empty;
        ProductStatus = new ProductStatusDefinition(Guid.Empty, string.Empty, string.Empty, false);
        Status = string.Empty;
        RowVersion = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Variant(
        Guid id,
        Guid productId,
        string sku,
        string? ean,
        string? mpn,
        string? barcode,
        ProductStatusDefinition productStatus,
        bool isDefaultVariant,
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        IEnumerable<VariantAttributeValue> attributeValues)
    {
        Id = id;
        ProductId = productId;
        Sku = sku;
        Ean = ean;
        Mpn = mpn;
        Barcode = barcode;
        ProductStatus = productStatus;
        IsDefaultVariant = isDefaultVariant;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Status = "Active";
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _attributeValues.AddRange(attributeValues);
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; }
    public string? Ean { get; private set; }
    public string? Mpn { get; private set; }
    public string? Barcode { get; private set; }
    public ProductStatusDefinition ProductStatus { get; private set; }
    public bool IsDefaultVariant { get; private set; }
    public string? PrimaryImageUrl { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyList<VariantAttributeValue> AttributeValues => _attributeValues;

    public void Update(
        string sku,
        string? ean,
        string? mpn,
        string? barcode,
        ProductStatusDefinition productStatus,
        bool isDefaultVariant,
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        IEnumerable<VariantAttributeValue> attributeValues,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Sku = sku;
        Ean = ean;
        Mpn = mpn;
        Barcode = barcode;
        ProductStatus = productStatus;
        IsDefaultVariant = isDefaultVariant;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        _attributeValues.Clear();
        _attributeValues.AddRange(attributeValues);
        Touch();
    }

    public void AssignStatus(ProductStatusDefinition productStatus)
    {
        ProductStatus = productStatus;
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The variant has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
