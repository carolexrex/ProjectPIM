using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Catalog.Channels;
using Platform.Contracts.Catalog.Categories;
using Platform.Contracts.Catalog.Markets;
using Platform.Contracts.Catalog.Media;
using Platform.Contracts.Catalog.Pricing;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Catalog.Variants;
using Platform.Contracts.Common;

namespace Platform.Backoffice.Integration;

public sealed partial class AdminApiClient : IAdminApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AdminApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResponse<BrandSummaryDto>> ListBrandsAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildBrandsPath(search, status, sort);
        return await GetRequiredAsync<PagedResponse<BrandSummaryDto>>(path, cancellationToken);
    }

    public async Task<BrandDetailsDto?> GetBrandAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/brands/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<BrandDetailsDto>(response, cancellationToken);
    }

    public async Task<BrandDetailsDto> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/brands", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<BrandDetailsDto>(response, cancellationToken);
    }

    public async Task<BrandDetailsDto?> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/brands/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<BrandDetailsDto>(response, cancellationToken);
    }

    public async Task<BrandDetailsDto?> ArchiveBrandAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/brands/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<BrandDetailsDto>(response, cancellationToken);
    }

    public async Task<BrandTranslationDto?> UpsertBrandTranslationAsync(
        Guid brandId,
        string cultureCode,
        UpsertBrandTranslationRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/admin/brands/{brandId}/translations/{Uri.EscapeDataString(cultureCode)}",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<BrandTranslationDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<ChannelSummaryDto>> ListChannelsAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildChannelsPath(search, status, sort);
        return await GetRequiredAsync<PagedResponse<ChannelSummaryDto>>(path, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> GetChannelAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/channels/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ChannelDetailsDto>(response, cancellationToken);
    }

    public async Task<ChannelDetailsDto> CreateChannelAsync(CreateChannelRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/channels", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ChannelDetailsDto>(response, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> UpdateChannelAsync(Guid id, UpdateChannelRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/channels/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ChannelDetailsDto>(response, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> ArchiveChannelAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/channels/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ChannelDetailsDto>(response, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> UpsertChannelMarketAssignmentAsync(Guid id, UpsertChannelMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/channels/{id}/markets", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ChannelDetailsDto>(response, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> RemoveChannelMarketAssignmentAsync(Guid id, Guid marketId, RemoveChannelMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/channels/{id}/markets/{marketId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ChannelDetailsDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<MediaAssetSummaryDto>> ListMediaAssetsAsync(
        string? search,
        string? status,
        string? contentType,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildMediaAssetsPath(search, status, contentType, sort);
        return await GetRequiredAsync<PagedResponse<MediaAssetSummaryDto>>(path, cancellationToken);
    }

    public async Task<MediaAssetDetailsDto?> GetMediaAssetAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/media-assets/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MediaAssetDetailsDto>(response, cancellationToken);
    }

    public async Task<MediaAssetDetailsDto> CreateMediaAssetAsync(CreateMediaAssetRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/media-assets", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MediaAssetDetailsDto>(response, cancellationToken);
    }

    public async Task<MediaAssetDetailsDto?> UpdateMediaAssetAsync(Guid id, UpdateMediaAssetRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/media-assets/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MediaAssetDetailsDto>(response, cancellationToken);
    }

    public async Task<MediaAssetDetailsDto?> ArchiveMediaAssetAsync(Guid id, ArchiveMediaAssetRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/media-assets/{id}/archive", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MediaAssetDetailsDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<PriceListSummaryDto>> ListPriceListsAsync(
        string? search,
        string? currencyCode,
        string? status,
        Guid? marketId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildPriceListsPath(search, currencyCode, status, marketId, sort);
        return await GetRequiredAsync<PagedResponse<PriceListSummaryDto>>(path, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> GetPriceListAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/price-lists/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto> CreatePriceListAsync(CreatePriceListRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/price-lists", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> UpdatePriceListAsync(Guid id, UpdatePriceListRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/price-lists/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> ArchivePriceListAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/price-lists/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> UpsertPriceListMarketAssignmentAsync(Guid id, UpsertPriceListMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/price-lists/{id}/markets", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> RemovePriceListMarketAssignmentAsync(Guid id, Guid marketId, RemovePriceListMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/price-lists/{id}/markets/{marketId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> UpsertPriceListEntryAsync(Guid id, UpsertPriceListEntryRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/price-lists/{id}/entries", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PriceListDetailsDto?> RemovePriceListEntryAsync(Guid id, Guid entryId, RemovePriceListEntryRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/price-lists/{id}/entries/{entryId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PriceListDetailsDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<MarketSummaryDto>> ListMarketsAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildMarketsPath(search, status, sort);
        return await GetRequiredAsync<PagedResponse<MarketSummaryDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<MarketLookupDto>> ListMarketLookupsAsync(
        string? search,
        string? status,
        string? currencyCode,
        CancellationToken cancellationToken)
    {
        var path = BuildMarketLookupsPath(search, status, currencyCode);
        return GetRequiredAsync<IReadOnlyList<MarketLookupDto>>(path, cancellationToken);
    }

    public async Task<MarketDetailsDto?> GetMarketAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/markets/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto> CreateMarketAsync(CreateMarketRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/markets", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto?> UpdateMarketAsync(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/markets/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto?> ArchiveMarketAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/markets/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto?> AssignMarketCurrenciesAsync(Guid id, AssignMarketCurrenciesRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/markets/{id}/currencies", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto?> AssignMarketCulturesAsync(Guid id, AssignMarketCulturesRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/markets/{id}/cultures", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto?> UpsertMarketProductAssignmentAsync(Guid id, Guid productId, UpsertMarketProductAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/markets/{id}/products/{productId}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<MarketDetailsDto?> RemoveMarketProductAssignmentAsync(Guid id, Guid productId, RemoveMarketProductAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/markets/{id}/products/{productId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<MarketDetailsDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<CategorySummaryDto>> ListCategoriesAsync(
        string? search,
        string? status,
        Guid? parentCategoryId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildCategoriesPath(search, status, parentCategoryId, sort);
        return await GetRequiredAsync<PagedResponse<CategorySummaryDto>>(path, cancellationToken);
    }

    public async Task<CategoryDetailsDto?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/categories/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CategoryDetailsDto>(response, cancellationToken);
    }

    public async Task<CategoryDetailsDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/categories", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CategoryDetailsDto>(response, cancellationToken);
    }

    public async Task<CategoryDetailsDto?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/categories/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CategoryDetailsDto>(response, cancellationToken);
    }

    public async Task<CategoryDetailsDto?> ArchiveCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/categories/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CategoryDetailsDto>(response, cancellationToken);
    }

    public async Task<CategoryTranslationDto?> UpsertCategoryTranslationAsync(
        Guid categoryId,
        string cultureCode,
        UpsertCategoryTranslationRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/admin/categories/{categoryId}/translations/{Uri.EscapeDataString(cultureCode)}",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CategoryTranslationDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<ProductAttributeSummaryDto>> ListProductAttributesAsync(
        string? search,
        string? status,
        string? scope,
        string? dataType,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildProductAttributesPath(search, status, scope, dataType, sort);
        return await GetRequiredAsync<PagedResponse<ProductAttributeSummaryDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<ProductAttributeEditorDefinitionDto>> ListProductAttributeEditorDefinitionsAsync(
        string scope,
        string? status,
        CancellationToken cancellationToken)
    {
        var path = BuildProductAttributeEditorDefinitionsPath(scope, status);
        return GetRequiredAsync<IReadOnlyList<ProductAttributeEditorDefinitionDto>>(path, cancellationToken);
    }

    public async Task<ProductAttributeDetailsDto?> GetProductAttributeAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/product-attributes/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductAttributeDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductAttributeDetailsDto> CreateProductAttributeAsync(CreateProductAttributeRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/product-attributes", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductAttributeDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductAttributeDetailsDto?> UpdateProductAttributeAsync(Guid id, UpdateProductAttributeRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/product-attributes/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductAttributeDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductAttributeDetailsDto?> ArchiveProductAttributeAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/product-attributes/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductAttributeDetailsDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<ProductSummaryDto>> ListProductsAsync(
        string? search,
        string? status,
        string? productStatusCode,
        bool? hasVariants,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildProductsPath(search, status, productStatusCode, hasVariants, sort);

        return await GetRequiredAsync<PagedResponse<ProductSummaryDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<ProductLookupDto>> ListProductLookupsAsync(
        string? search,
        string? status,
        bool? hasVariants,
        Guid? excludedProductId,
        CancellationToken cancellationToken)
    {
        var path = BuildProductLookupsPath(search, status, hasVariants, excludedProductId);
        return GetRequiredAsync<IReadOnlyList<ProductLookupDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<ProductStatusDto>> ListProductStatusesAsync(CancellationToken cancellationToken)
    {
        return GetRequiredAsync<IReadOnlyList<ProductStatusDto>>("api/admin/product-statuses?entityType=product", cancellationToken);
    }

    public Task<IReadOnlyList<ProductStatusDto>> ListVariantStatusesAsync(CancellationToken cancellationToken)
    {
        return GetRequiredAsync<IReadOnlyList<ProductStatusDto>>("api/admin/product-statuses?entityType=variant", cancellationToken);
    }

    public async Task<ProductDetailsDto?> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/products/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/products", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/products/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto?> ArchiveProductAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/products/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto?> UpsertProductMediaAsync(Guid id, UpsertProductMediaRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/products/{id}/media", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto?> RemoveProductMediaAsync(Guid id, Guid productMediaId, RemoveProductMediaRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/products/{id}/media/{productMediaId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto?> UpsertProductRelationAsync(Guid id, UpsertProductRelationRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/products/{id}/relations", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductDetailsDto?> RemoveProductRelationAsync(Guid id, Guid relationId, RemoveProductRelationRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/products/{id}/relations/{relationId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductDetailsDto>(response, cancellationToken);
    }

    public async Task<ProductTranslationDto?> UpsertProductTranslationAsync(
        Guid productId,
        string cultureCode,
        UpsertProductTranslationRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/admin/products/{productId}/translations/{Uri.EscapeDataString(cultureCode)}",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<ProductTranslationDto>(response, cancellationToken);
    }

    public Task<IReadOnlyList<VariantLookupDto>> ListVariantLookupsAsync(
        string? search,
        string? status,
        Guid? productId,
        CancellationToken cancellationToken)
    {
        var path = BuildVariantLookupsPath(search, status, productId);
        return GetRequiredAsync<IReadOnlyList<VariantLookupDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<VariantSummaryDto>> ListVariantsAsync(Guid productId, CancellationToken cancellationToken)
    {
        return GetRequiredAsync<IReadOnlyList<VariantSummaryDto>>($"api/admin/products/{productId}/variants", cancellationToken);
    }

    public async Task<VariantDetailsDto?> GetVariantAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/variants/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<VariantDetailsDto>(response, cancellationToken);
    }

    public async Task<VariantDetailsDto> CreateVariantAsync(Guid productId, CreateVariantRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/products/{productId}/variants", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<VariantDetailsDto>(response, cancellationToken);
    }

    public async Task<VariantDetailsDto?> UpdateVariantAsync(Guid id, UpdateVariantRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/variants/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<VariantDetailsDto>(response, cancellationToken);
    }

    public async Task<VariantDetailsDto?> UpsertVariantMediaAsync(Guid id, UpsertVariantMediaRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/variants/{id}/media", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<VariantDetailsDto>(response, cancellationToken);
    }

    public async Task<VariantDetailsDto?> RemoveVariantMediaAsync(Guid id, Guid variantMediaId, RemoveVariantMediaRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/variants/{id}/media/{variantMediaId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<VariantDetailsDto>(response, cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        if (value is null)
        {
            throw new AdminApiException("The admin API returned an empty response.", (int)response.StatusCode);
        }

        return value;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemResponse>(JsonOptions, cancellationToken);
        var message = problem?.Detail ?? problem?.Title ?? $"Admin API request failed with status {(int)response.StatusCode}.";

        throw new AdminApiException(message, (int)response.StatusCode, problem?.Errors);
    }

    private static string BuildProductsPath(
        string? search,
        string? status,
        string? productStatusCode,
        bool? hasVariants,
        string? sort)
    {
        var query = new List<string>();

        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "productStatusCode", productStatusCode);
        AddQuery(query, "hasVariants", hasVariants?.ToString()?.ToLowerInvariant());
        AddQuery(query, "sort", sort);

        if (query.Count == 0)
        {
            return "api/admin/products";
        }

        var builder = new StringBuilder("api/admin/products?");
        builder.Append(string.Join("&", query));
        return builder.ToString();
    }

    private static void AddQuery(List<string> query, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
    }

    private static string BuildCategoriesPath(string? search, string? status, Guid? parentCategoryId, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "parentCategoryId", parentCategoryId?.ToString());
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/categories" : $"api/admin/categories?{string.Join("&", query)}";
    }

    private static string BuildBrandsPath(string? search, string? status, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/brands" : $"api/admin/brands?{string.Join("&", query)}";
    }

    private static string BuildChannelsPath(string? search, string? status, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/channels" : $"api/admin/channels?{string.Join("&", query)}";
    }

    private static string BuildMarketsPath(string? search, string? status, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/markets" : $"api/admin/markets?{string.Join("&", query)}";
    }

    private static string BuildMarketLookupsPath(string? search, string? status, string? currencyCode)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "currencyCode", currencyCode);
        return query.Count == 0 ? "api/admin/markets/lookup" : $"api/admin/markets/lookup?{string.Join("&", query)}";
    }

    private static string BuildPriceListsPath(string? search, string? currencyCode, string? status, Guid? marketId, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "currencyCode", currencyCode);
        AddQuery(query, "status", status);
        AddQuery(query, "marketId", marketId?.ToString());
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/price-lists" : $"api/admin/price-lists?{string.Join("&", query)}";
    }

    private static string BuildMediaAssetsPath(string? search, string? status, string? contentType, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "contentType", contentType);
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/media-assets" : $"api/admin/media-assets?{string.Join("&", query)}";
    }

    private static string BuildProductAttributesPath(string? search, string? status, string? scope, string? dataType, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "scope", scope);
        AddQuery(query, "dataType", dataType);
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/product-attributes" : $"api/admin/product-attributes?{string.Join("&", query)}";
    }

    private static string BuildProductAttributeEditorDefinitionsPath(string scope, string? status)
    {
        var query = new List<string>();
        AddQuery(query, "scope", scope);
        AddQuery(query, "status", status);
        return $"api/admin/product-attributes/editor-definitions?{string.Join("&", query)}";
    }

    private static string BuildProductLookupsPath(string? search, string? status, bool? hasVariants, Guid? excludedProductId)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "hasVariants", hasVariants?.ToString()?.ToLowerInvariant());
        AddQuery(query, "excludedProductId", excludedProductId?.ToString());
        return query.Count == 0 ? "api/admin/products/lookup" : $"api/admin/products/lookup?{string.Join("&", query)}";
    }

    private static string BuildVariantLookupsPath(string? search, string? status, Guid? productId)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "productId", productId?.ToString());
        return query.Count == 0 ? "api/admin/variants/lookup" : $"api/admin/variants/lookup?{string.Join("&", query)}";
    }

    private sealed class ApiProblemResponse
    {
        public string? Title { get; init; }
        public string? Detail { get; init; }
        public Dictionary<string, string[]>? Errors { get; init; }
    }
}
