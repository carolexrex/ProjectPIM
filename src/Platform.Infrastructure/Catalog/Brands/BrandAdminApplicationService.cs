using System.Text.Json;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Brands.Commands;
using Platform.Application.Catalog.Brands.Queries;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Products;
using Platform.Application.Integrations;
using Platform.Application.Storefront;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Domain.Catalog.Brands;
using Platform.Domain.Catalog.Media;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Catalog.Brands;

public sealed class BrandAdminApplicationService : IBrandAdminApplicationService
{
    private readonly IBrandRepository _brandRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IOutboxEventPublisher _outboxEventPublisher;
    private readonly IProductRepository _productRepository;
    private readonly IStorefrontProjectionRefreshRequestPublisher _storefrontProjectionRefreshRequestPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public BrandAdminApplicationService(
        IBrandRepository brandRepository,
        IMediaAssetRepository mediaAssetRepository,
        IOutboxEventPublisher outboxEventPublisher,
        IProductRepository productRepository,
        IStorefrontProjectionRefreshRequestPublisher storefrontProjectionRefreshRequestPublisher,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _outboxEventPublisher = outboxEventPublisher;
        _productRepository = productRepository;
        _storefrontProjectionRefreshRequestPublisher = storefrontProjectionRefreshRequestPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<BrandSummaryDto>> ListAsync(ListBrandsQuery query, CancellationToken cancellationToken)
    {
        var result = await _brandRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var logoIds = result.Items.Where(x => x.LogoMediaAssetId is not null).Select(x => x.LogoMediaAssetId!.Value).Distinct().ToList();
        var logos = logoIds.Count == 0 ? [] : await _mediaAssetRepository.GetByIdsAsync(logoIds, cancellationToken);
        var logoMap = logos.ToDictionary(x => x.Id);

        return new PagedResponse<BrandSummaryDto>(
            result.Items.Select(x => MapSummary(x, logoMap)).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<BrandDetailsDto?> GetByIdAsync(GetBrandByIdQuery query, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(query.BrandId, cancellationToken);
        if (brand is null)
        {
            return null;
        }

        var logo = await ResolveLogoAsync(brand.LogoMediaAssetId, cancellationToken);
        return MapDetails(brand, logo);
    }

    public async Task<BrandDetailsDto> CreateAsync(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeIsUniqueAsync(command.Code, null, cancellationToken);
        await EnsureLogoExistsAsync(command.LogoMediaAssetId, cancellationToken);

        var now = DateTime.UtcNow;
        var brand = new Brand(Guid.NewGuid(), command.Code, command.WebsiteUrl, command.LogoMediaAssetId, command.SortOrder, now, now);

        await _brandRepository.AddAsync(brand, cancellationToken);
        var logo = await ResolveLogoAsync(brand.LogoMediaAssetId, cancellationToken);
        var details = MapDetails(brand, logo);
        await PublishEventAsync(WebhookEventTypes.BrandCreated, "Created", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<BrandDetailsDto?> UpdateAsync(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
        {
            return null;
        }

        await EnsureCodeIsUniqueAsync(command.Code, command.BrandId, cancellationToken);
        await EnsureLogoExistsAsync(command.LogoMediaAssetId, cancellationToken);

        brand.Update(command.Code, command.WebsiteUrl, command.LogoMediaAssetId, command.SortOrder, command.RowVersion);
        var logo = await ResolveLogoAsync(brand.LogoMediaAssetId, cancellationToken);
        var details = MapDetails(brand, logo);
        await PublishEventAsync(WebhookEventTypes.BrandUpdated, "Updated", details, cancellationToken);
        await EnqueueStorefrontRefreshAsync(brand.Id, "BrandUpdated", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<BrandDetailsDto?> ArchiveAsync(ArchiveBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
        {
            return null;
        }

        brand.Archive();
        var logo = await ResolveLogoAsync(brand.LogoMediaAssetId, cancellationToken);
        var details = MapDetails(brand, logo);
        await PublishEventAsync(WebhookEventTypes.BrandUpdated, "Archived", details, cancellationToken);
        await EnqueueStorefrontRefreshAsync(brand.Id, "BrandArchived", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<BrandTranslationDto?> UpsertTranslationAsync(UpsertBrandTranslationCommand command, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
        {
            return null;
        }

        var translation = brand.UpsertTranslation(command.CultureCode, command.Name, command.Slug, command.Description);
        var logo = await ResolveLogoAsync(brand.LogoMediaAssetId, cancellationToken);
        var details = MapDetails(brand, logo);
        await PublishEventAsync(WebhookEventTypes.BrandUpdated, "TranslationUpserted", details, cancellationToken);
        await EnqueueStorefrontRefreshAsync(brand.Id, "BrandTranslationUpserted", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTranslation(translation);
    }

    private Task PublishEventAsync(
        string eventType,
        string changeType,
        BrandDetailsDto details,
        CancellationToken cancellationToken)
    {
        var payload = new BrandWebhookEventDto(DateTime.UtcNow, changeType, details);
        return _outboxEventPublisher.EnqueueAsync(
            eventType,
            "Brand",
            details.Id,
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }

    private async Task EnqueueStorefrontRefreshAsync(Guid brandId, string reason, CancellationToken cancellationToken)
    {
        var productIds = await _productRepository.ListIdsByBrandIdAsync(brandId, cancellationToken);
        foreach (var productId in productIds)
        {
            await _storefrontProjectionRefreshRequestPublisher.EnqueueProductRefreshAsync(productId, reason, cancellationToken);
        }
    }

    private async Task EnsureCodeIsUniqueAsync(string code, Guid? currentBrandId, CancellationToken cancellationToken)
    {
        var existing = await _brandRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != currentBrandId)
        {
            throw new ConflictException("Brand code already exists.");
        }
    }

    private async Task EnsureLogoExistsAsync(Guid? logoMediaAssetId, CancellationToken cancellationToken)
    {
        if (logoMediaAssetId is null)
        {
            return;
        }

        if (await _mediaAssetRepository.GetByIdAsync(logoMediaAssetId.Value, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(CreateBrandCommand.LogoMediaAssetId), "Unknown media asset.");
        }
    }

    private async Task<MediaAsset?> ResolveLogoAsync(Guid? logoMediaAssetId, CancellationToken cancellationToken)
    {
        return logoMediaAssetId is null
            ? null
            : await _mediaAssetRepository.GetByIdAsync(logoMediaAssetId.Value, cancellationToken);
    }

    private static BrandSummaryDto MapSummary(Brand brand, IReadOnlyDictionary<Guid, MediaAsset> logoMap)
    {
        var logo = brand.LogoMediaAssetId is Guid logoId ? logoMap.GetValueOrDefault(logoId) : null;
        return new BrandSummaryDto(
            brand.Id,
            brand.Code,
            brand.Translations.FirstOrDefault()?.Name,
            brand.WebsiteUrl,
            brand.LogoMediaAssetId,
            logo?.PublicUrl,
            brand.SortOrder,
            brand.Status,
            brand.CreatedAtUtc,
            brand.UpdatedAtUtc,
            brand.RowVersion);
    }

    private static BrandDetailsDto MapDetails(Brand brand, MediaAsset? logo)
    {
        return new BrandDetailsDto(
            brand.Id,
            brand.Code,
            brand.WebsiteUrl,
            brand.LogoMediaAssetId,
            logo?.FileName,
            logo?.PublicUrl,
            brand.SortOrder,
            brand.Status,
            brand.Translations.Select(MapTranslation).ToList(),
            brand.CreatedAtUtc,
            brand.UpdatedAtUtc,
            brand.RowVersion);
    }

    private static BrandTranslationDto MapTranslation(BrandTranslation translation)
    {
        return new BrandTranslationDto(translation.CultureCode, translation.Name, translation.Slug, translation.Description);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
