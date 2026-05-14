using Platform.Contracts.Cart;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Catalog.Channels;
using Platform.Contracts.Catalog.Categories;
using Platform.Contracts.Catalog.Markets;
using Platform.Contracts.Catalog.Media;
using Platform.Contracts.Catalog.Pricing;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Catalog.Variants;
using Platform.Contracts.Companies;
using Platform.Contracts.Common;
using Platform.Contracts.Customers;
using Platform.Contracts.Orders;

namespace Platform.Backoffice.Integration;

public interface IAdminApiClient
{
    Task<PagedResponse<BrandSummaryDto>> ListBrandsAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken);
    Task<BrandDetailsDto?> GetBrandAsync(Guid id, CancellationToken cancellationToken);
    Task<BrandDetailsDto> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken);
    Task<BrandDetailsDto?> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken);
    Task<BrandDetailsDto?> ArchiveBrandAsync(Guid id, CancellationToken cancellationToken);
    Task<BrandTranslationDto?> UpsertBrandTranslationAsync(
        Guid brandId,
        string cultureCode,
        UpsertBrandTranslationRequest request,
        CancellationToken cancellationToken);
    Task<PagedResponse<CartSummaryDto>> ListCartsAsync(
        string? status,
        Guid? customerId,
        Guid? companyId,
        Guid? marketId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? sort,
        CancellationToken cancellationToken);
    Task<CartDetailsDto?> GetCartAsync(Guid id, CancellationToken cancellationToken);
    Task<CartDetailsDto?> RepriceCartAsync(Guid id, RepriceCartRequest request, CancellationToken cancellationToken);
    Task<CartDetailsDto?> ExpireCartAsync(Guid id, ExpireCartRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<CustomerSummaryDto>> ListCustomersAsync(
        string? search,
        string? status,
        bool? isGuest,
        Guid? defaultMarketId,
        string? sort,
        CancellationToken cancellationToken);
    Task<CustomerDetailsDto?> GetCustomerAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomerDetailsDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerDetailsDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerAddressDto?> AddCustomerAddressAsync(Guid id, AddCustomerAddressRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<CompanySummaryDto>> ListCompaniesAsync(
        string? search,
        string? status,
        Guid? defaultMarketId,
        string? sort,
        CancellationToken cancellationToken);
    Task<CompanyDetailsDto?> GetCompanyAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyMembershipDto>?> ListCompanyMembershipsAsync(Guid id, CancellationToken cancellationToken);
    Task<CompanyDetailsDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken);
    Task<CompanyDetailsDto?> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken);
    Task<CompanyAddressDto?> AddCompanyAddressAsync(Guid id, AddCompanyAddressRequest request, CancellationToken cancellationToken);
    Task<CompanyMembershipDto?> CreateCompanyMembershipAsync(Guid id, CreateCompanyMembershipRequest request, CancellationToken cancellationToken);
    Task<CompanyMembershipDto?> UpdateCompanyMembershipAsync(Guid id, UpdateCompanyMembershipRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<ChannelSummaryDto>> ListChannelsAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> GetChannelAsync(Guid id, CancellationToken cancellationToken);
    Task<ChannelDetailsDto> CreateChannelAsync(CreateChannelRequest request, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> UpdateChannelAsync(Guid id, UpdateChannelRequest request, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> ArchiveChannelAsync(Guid id, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> UpsertChannelMarketAssignmentAsync(Guid id, UpsertChannelMarketAssignmentRequest request, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> RemoveChannelMarketAssignmentAsync(Guid id, Guid marketId, RemoveChannelMarketAssignmentRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<InventoryLocationSummaryDto>> ListInventoryLocationsAsync(
        string? search,
        string? status,
        Guid? marketId,
        string? sort,
        CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> GetInventoryLocationAsync(Guid id, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto> CreateInventoryLocationAsync(CreateInventoryLocationRequest request, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> UpdateInventoryLocationAsync(Guid id, UpdateInventoryLocationRequest request, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> ArchiveInventoryLocationAsync(Guid id, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> UpsertInventoryLocationMarketAssignmentAsync(Guid id, UpsertInventoryLocationMarketAssignmentRequest request, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> RemoveInventoryLocationMarketAssignmentAsync(Guid id, Guid marketId, RemoveInventoryLocationMarketAssignmentRequest request, CancellationToken cancellationToken);
    Task<InventoryBalanceDto> UpsertInventoryBalanceAsync(UpsertInventoryBalanceRequest request, CancellationToken cancellationToken);
    Task<InventoryTransactionDto> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<MediaAssetSummaryDto>> ListMediaAssetsAsync(
        string? search,
        string? status,
        string? contentType,
        string? sort,
        CancellationToken cancellationToken);
    Task<PagedResponse<MarketSummaryDto>> ListMarketsAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketLookupDto>> ListMarketLookupsAsync(
        string? search,
        string? status,
        string? currencyCode,
        CancellationToken cancellationToken);
    Task<MarketDetailsDto?> GetMarketAsync(Guid id, CancellationToken cancellationToken);
    Task<MarketDetailsDto> CreateMarketAsync(CreateMarketRequest request, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> UpdateMarketAsync(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> ArchiveMarketAsync(Guid id, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> AssignMarketCurrenciesAsync(Guid id, AssignMarketCurrenciesRequest request, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> AssignMarketCulturesAsync(Guid id, AssignMarketCulturesRequest request, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> UpsertMarketProductAssignmentAsync(Guid id, Guid productId, UpsertMarketProductAssignmentRequest request, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> RemoveMarketProductAssignmentAsync(Guid id, Guid productId, RemoveMarketProductAssignmentRequest request, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto?> GetMediaAssetAsync(Guid id, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto> CreateMediaAssetAsync(CreateMediaAssetRequest request, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto?> UpdateMediaAssetAsync(Guid id, UpdateMediaAssetRequest request, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto?> ArchiveMediaAssetAsync(Guid id, ArchiveMediaAssetRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<OrderSummaryDto>> ListOrdersAsync(
        string? status,
        string? paymentStatus,
        string? fulfillmentStatus,
        Guid? customerId,
        Guid? companyId,
        Guid? marketId,
        DateTime? placedFromUtc,
        DateTime? placedToUtc,
        string? search,
        string? sort,
        CancellationToken cancellationToken);
    Task<OrderDetailsDto?> GetOrderAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderStatusHistoryDto>?> GetOrderStatusHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderDetailsDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderStatusHistoryDto?> ChangeOrderStatusAsync(Guid id, ChangeOrderStatusRequest request, CancellationToken cancellationToken);
    Task<PaymentTransactionDto?> AddOrderPaymentTransactionAsync(Guid id, AddPaymentTransactionRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<PriceListSummaryDto>> ListPriceListsAsync(
        string? search,
        string? currencyCode,
        string? status,
        Guid? marketId,
        string? sort,
        CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> GetPriceListAsync(Guid id, CancellationToken cancellationToken);
    Task<PriceListDetailsDto> CreatePriceListAsync(CreatePriceListRequest request, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> UpdatePriceListAsync(Guid id, UpdatePriceListRequest request, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> ArchivePriceListAsync(Guid id, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> UpsertPriceListMarketAssignmentAsync(Guid id, UpsertPriceListMarketAssignmentRequest request, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> RemovePriceListMarketAssignmentAsync(Guid id, Guid marketId, RemovePriceListMarketAssignmentRequest request, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> UpsertPriceListEntryAsync(Guid id, UpsertPriceListEntryRequest request, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> RemovePriceListEntryAsync(Guid id, Guid entryId, RemovePriceListEntryRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<CategorySummaryDto>> ListCategoriesAsync(
        string? search,
        string? status,
        Guid? parentCategoryId,
        string? sort,
        CancellationToken cancellationToken);
    Task<CategoryDetailsDto?> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryDetailsDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDetailsDto?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDetailsDto?> ArchiveCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryTranslationDto?> UpsertCategoryTranslationAsync(
        Guid categoryId,
        string cultureCode,
        UpsertCategoryTranslationRequest request,
        CancellationToken cancellationToken);
    Task<PagedResponse<ProductAttributeSummaryDto>> ListProductAttributesAsync(
        string? search,
        string? status,
        string? scope,
        string? dataType,
        string? sort,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductAttributeEditorDefinitionDto>> ListProductAttributeEditorDefinitionsAsync(
        string scope,
        string? status,
        CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto?> GetProductAttributeAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto> CreateProductAttributeAsync(CreateProductAttributeRequest request, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto?> UpdateProductAttributeAsync(Guid id, UpdateProductAttributeRequest request, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto?> ArchiveProductAttributeAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<ProductSummaryDto>> ListProductsAsync(
        string? search,
        string? status,
        string? productStatusCode,
        bool? hasVariants,
        string? sort,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductLookupDto>> ListProductLookupsAsync(
        string? search,
        string? status,
        bool? hasVariants,
        Guid? excludedProductId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductStatusDto>> ListProductStatusesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductStatusDto>> ListVariantStatusesAsync(CancellationToken cancellationToken);
    Task<ProductDetailsDto?> GetProductAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDetailsDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> ArchiveProductAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> UpsertProductMediaAsync(Guid id, UpsertProductMediaRequest request, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> RemoveProductMediaAsync(Guid id, Guid productMediaId, RemoveProductMediaRequest request, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> UpsertProductRelationAsync(Guid id, UpsertProductRelationRequest request, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> RemoveProductRelationAsync(Guid id, Guid relationId, RemoveProductRelationRequest request, CancellationToken cancellationToken);
    Task<ProductTranslationDto?> UpsertProductTranslationAsync(
        Guid productId,
        string cultureCode,
        UpsertProductTranslationRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<VariantLookupDto>> ListVariantLookupsAsync(
        string? search,
        string? status,
        Guid? productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<VariantSummaryDto>> ListVariantsAsync(Guid productId, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> GetVariantAsync(Guid id, CancellationToken cancellationToken);
    Task<VariantInventorySnapshotDto?> GetVariantInventorySnapshotAsync(Guid id, CancellationToken cancellationToken);
    Task<VariantDetailsDto> CreateVariantAsync(Guid productId, CreateVariantRequest request, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> UpdateVariantAsync(Guid id, UpdateVariantRequest request, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> UpsertVariantMediaAsync(Guid id, UpsertVariantMediaRequest request, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> RemoveVariantMediaAsync(Guid id, Guid variantMediaId, RemoveVariantMediaRequest request, CancellationToken cancellationToken);
}
