using Platform.Domain.Common;

using Platform.Domain.Catalog.Products;

namespace Platform.Domain.Catalog.Variants;

public sealed class Variant
{
    private readonly List<VariantAttributeValue> _attributeValues = [];
    private readonly List<VariantMedia> _media = [];

    private Variant()
    {
        Id = Guid.Empty;
        ProductId = Guid.Empty;
        Sku = string.Empty;
        ProductStatus = new ProductStatusDefinition(Guid.Empty, ProductStatusEntityType.Variant, string.Empty, string.Empty, false);
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
    public IReadOnlyList<VariantMedia> Media => _media;

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

    public void UpsertMedia(Guid mediaAssetId, string type, int sortOrder, bool isPrimary, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _media.FirstOrDefault(x => x.MediaAssetId == mediaAssetId && string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            _media.Add(new VariantMedia(Guid.NewGuid(), mediaAssetId, type, sortOrder, isPrimary));
        }
        else
        {
            existing.Update(sortOrder, isPrimary);
        }

        NormalizeMediaPrimary(isPrimary, mediaAssetId, type);
        SortMedia();
        Touch();
    }

    public void RemoveMedia(Guid variantMediaId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var media = _media.FirstOrDefault(x => x.Id == variantMediaId);
        if (media is null)
        {
            return;
        }

        _media.Remove(media);
        if (_media.Count > 0 && !_media.Any(x => x.IsPrimary))
        {
            _media[0].Update(_media[0].SortOrder, true);
        }

        SortMedia();
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

    private void NormalizeMediaPrimary(bool isPrimary, Guid mediaAssetId, string type)
    {
        if (isPrimary)
        {
            foreach (var media in _media)
            {
                var shouldBePrimary = media.MediaAssetId == mediaAssetId && string.Equals(media.Type, type, StringComparison.OrdinalIgnoreCase);
                media.Update(media.SortOrder, shouldBePrimary);
            }

            return;
        }

        if (_media.All(x => !x.IsPrimary))
        {
            _media[0].Update(_media[0].SortOrder, true);
        }
    }

    private void SortMedia()
    {
        var ordered = _media
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.MediaAssetId)
            .ToList();

        _media.Clear();
        _media.AddRange(ordered);
    }
}
