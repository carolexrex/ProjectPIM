namespace Platform.Application.Storefront;

public sealed class StorefrontProductProjection
{
    private StorefrontProductProjection()
    {
        Id = Guid.Empty;
        ProductId = Guid.Empty;
        MarketId = Guid.Empty;
        MarketCode = string.Empty;
        CultureCode = string.Empty;
        CurrencyCode = string.Empty;
        ProductNumber = string.Empty;
        Slug = string.Empty;
        ProductType = string.Empty;
        Name = string.Empty;
        AvailabilityStatus = string.Empty;
        BuyabilityReasonsJson = "[]";
        CategoryCodesJson = "[]";
        CategorySlugsJson = "[]";
        CategoryNamesJson = "[]";
        CategoryFilterSlugsJson = "[]";
        CategoriesJson = "[]";
        AttributesJson = "[]";
        MediaJson = "[]";
        SearchText = string.Empty;
        SortName = string.Empty;
        SortProductNumber = string.Empty;
        VariantsJson = "[]";
        ProjectionVersion = string.Empty;
        RowVersion = string.Empty;
    }

    public StorefrontProductProjection(
        Guid id,
        Guid productId,
        Guid marketId,
        string marketCode,
        string cultureCode,
        string currencyCode,
        string productNumber,
        string slug,
        string productType,
        string name,
        string? shortDescription,
        string? longDescription,
        string? seoTitle,
        string? seoDescription,
        Guid? brandId,
        string? brandCode,
        string? brandName,
        string? brandSlug,
        string? brandWebsiteUrl,
        string? brandLogoUrl,
        string categoryCodesJson,
        string categorySlugsJson,
        string categoryNamesJson,
        string categoryFilterSlugsJson,
        string categoriesJson,
        string? primaryImageUrl,
        string attributesJson,
        string mediaJson,
        bool hasVariants,
        bool isVisible,
        bool isBuyable,
        string buyabilityReasonsJson,
        string availabilityStatus,
        decimal availableQuantity,
        bool isBackorderable,
        decimal? priceAmount,
        decimal? compareAtAmount,
        bool? vatIncluded,
        string? priceListCode,
        string variantsJson,
        string searchText,
        string sortName,
        string sortProductNumber,
        decimal? sortPriceAmount,
        string? brandSortName,
        DateTime sourceUpdatedAtUtc,
        DateTime projectedAtUtc)
    {
        Id = id;
        ProductId = productId;
        MarketId = marketId;
        MarketCode = marketCode;
        CultureCode = cultureCode;
        CurrencyCode = currencyCode;
        ProductNumber = productNumber;
        Slug = slug;
        ProductType = productType;
        Name = name;
        ShortDescription = shortDescription;
        LongDescription = longDescription;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
        BrandId = brandId;
        BrandCode = brandCode;
        BrandName = brandName;
        BrandSlug = brandSlug;
        BrandWebsiteUrl = brandWebsiteUrl;
        BrandLogoUrl = brandLogoUrl;
        CategoryCodesJson = categoryCodesJson;
        CategorySlugsJson = categorySlugsJson;
        CategoryNamesJson = categoryNamesJson;
        CategoryFilterSlugsJson = categoryFilterSlugsJson;
        CategoriesJson = categoriesJson;
        PrimaryImageUrl = primaryImageUrl;
        AttributesJson = attributesJson;
        MediaJson = mediaJson;
        HasVariants = hasVariants;
        IsVisible = isVisible;
        IsBuyable = isBuyable;
        BuyabilityReasonsJson = buyabilityReasonsJson;
        AvailabilityStatus = availabilityStatus;
        AvailableQuantity = availableQuantity;
        IsBackorderable = isBackorderable;
        PriceAmount = priceAmount;
        CompareAtAmount = compareAtAmount;
        VatIncluded = vatIncluded;
        PriceListCode = priceListCode;
        VariantsJson = variantsJson;
        SearchText = searchText;
        SortName = sortName;
        SortProductNumber = sortProductNumber;
        SortPriceAmount = sortPriceAmount;
        BrandSortName = brandSortName;
        SourceUpdatedAtUtc = sourceUpdatedAtUtc;
        ProjectedAtUtc = projectedAtUtc;
        ProjectionVersion = "v1";
        CreatedAtUtc = projectedAtUtc;
        UpdatedAtUtc = projectedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid MarketId { get; private set; }
    public string MarketCode { get; private set; }
    public string CultureCode { get; private set; }
    public string CurrencyCode { get; private set; }
    public string ProductNumber { get; private set; }
    public string Slug { get; private set; }
    public string ProductType { get; private set; }
    public string Name { get; private set; }
    public string? ShortDescription { get; private set; }
    public string? LongDescription { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public Guid? BrandId { get; private set; }
    public string? BrandCode { get; private set; }
    public string? BrandName { get; private set; }
    public string? BrandSlug { get; private set; }
    public string? BrandWebsiteUrl { get; private set; }
    public string? BrandLogoUrl { get; private set; }
    public string CategoryCodesJson { get; private set; }
    public string CategorySlugsJson { get; private set; }
    public string CategoryNamesJson { get; private set; }
    public string CategoryFilterSlugsJson { get; private set; }
    public string CategoriesJson { get; private set; }
    public string? PrimaryImageUrl { get; private set; }
    public string AttributesJson { get; private set; }
    public string MediaJson { get; private set; }
    public bool HasVariants { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsBuyable { get; private set; }
    public string BuyabilityReasonsJson { get; private set; }
    public string AvailabilityStatus { get; private set; }
    public decimal AvailableQuantity { get; private set; }
    public bool IsBackorderable { get; private set; }
    public decimal? PriceAmount { get; private set; }
    public decimal? CompareAtAmount { get; private set; }
    public bool? VatIncluded { get; private set; }
    public string? PriceListCode { get; private set; }
    public string VariantsJson { get; private set; }
    public string SearchText { get; private set; }
    public string SortName { get; private set; }
    public string SortProductNumber { get; private set; }
    public decimal? SortPriceAmount { get; private set; }
    public string? BrandSortName { get; private set; }
    public DateTime SourceUpdatedAtUtc { get; private set; }
    public DateTime ProjectedAtUtc { get; private set; }
    public string ProjectionVersion { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
}
