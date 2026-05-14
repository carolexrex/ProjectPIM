using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Products;

public sealed class Product
{
    private readonly List<ProductTranslation> _translations = [];
    private readonly List<ProductCategoryAssignment> _categoryAssignments = [];
    private readonly List<ProductAttributeValue> _attributeValues = [];
    private readonly List<ProductMedia> _media = [];
    private readonly List<ProductRelation> _relations = [];

    private Product()
    {
        Id = Guid.Empty;
        ProductType = string.Empty;
        ProductNumber = string.Empty;
        Slug = string.Empty;
        ProductStatus = new ProductStatusDefinition(Guid.Empty, ProductStatusEntityType.Product, string.Empty, string.Empty, false);
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
        IEnumerable<Guid> categoryIds,
        IEnumerable<ProductAttributeValue> attributeValues,
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
        ReplaceCategoryAssignments(categoryIds);
        ReplaceAttributeValues(attributeValues);
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
    public IReadOnlyCollection<ProductCategoryAssignment> CategoryAssignments => _categoryAssignments;
    public IReadOnlyCollection<ProductAttributeValue> AttributeValues => _attributeValues;
    public IReadOnlyCollection<ProductMedia> Media => _media;
    public IReadOnlyCollection<ProductRelation> Relations => _relations;

    public void Update(
        string productType,
        string slug,
        Guid? brandId,
        ProductStatusDefinition productStatus,
        string taxCategoryCode,
        string unitOfMeasure,
        IEnumerable<Guid> categoryIds,
        IEnumerable<ProductAttributeValue> attributeValues,
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
        ReplaceCategoryAssignments(categoryIds);
        ReplaceAttributeValues(attributeValues);
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

    public void AssignCategories(IEnumerable<Guid> categoryIds, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        ReplaceCategoryAssignments(categoryIds);
        Touch();
    }

    public void UpsertRelation(Guid targetProductId, string relationType, decimal? quantity, int sortOrder, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _relations.FirstOrDefault(x =>
            x.TargetProductId == targetProductId
            && string.Equals(x.RelationType, relationType, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            _relations.Add(new ProductRelation(Guid.NewGuid(), targetProductId, relationType, quantity, sortOrder));
        }
        else
        {
            existing.Update(quantity, sortOrder);
        }

        SortRelations();
        Touch();
    }

    public void UpsertMedia(Guid mediaAssetId, string type, int sortOrder, bool isPrimary, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _media.FirstOrDefault(x => x.MediaAssetId == mediaAssetId && string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            _media.Add(new ProductMedia(Guid.NewGuid(), mediaAssetId, type, sortOrder, isPrimary));
        }
        else
        {
            existing.Update(sortOrder, isPrimary);
        }

        NormalizeMediaPrimary(isPrimary, mediaAssetId, type);
        SortMedia();
        Touch();
    }

    public void RemoveMedia(Guid productMediaId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var media = _media.FirstOrDefault(x => x.Id == productMediaId);
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

    public void RemoveRelation(Guid relationId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var relation = _relations.FirstOrDefault(x => x.Id == relationId);
        if (relation is null)
        {
            return;
        }

        _relations.Remove(relation);
        Touch();
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

    private void ReplaceCategoryAssignments(IEnumerable<Guid> categoryIds)
    {
        var distinctCategoryIds = categoryIds
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        _categoryAssignments.Clear();
        _categoryAssignments.AddRange(
            distinctCategoryIds.Select(categoryId => new ProductCategoryAssignment(Guid.NewGuid(), categoryId)));
    }

    private void ReplaceAttributeValues(IEnumerable<ProductAttributeValue> attributeValues)
    {
        var nextValues = attributeValues
            .OrderBy(x => x.ProductAttributeId)
            .ToList();

        var duplicateAttribute = nextValues
            .GroupBy(x => x.ProductAttributeId)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateAttribute is not null)
        {
            throw new InvalidOperationException($"Duplicate product attribute value '{duplicateAttribute.Key}'.");
        }

        _attributeValues.Clear();
        _attributeValues.AddRange(nextValues);
    }

    private void SortRelations()
    {
        var ordered = _relations
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.RelationType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.TargetProductId)
            .ToList();

        _relations.Clear();
        _relations.AddRange(ordered);
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
