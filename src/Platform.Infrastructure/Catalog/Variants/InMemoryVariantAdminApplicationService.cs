using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Variants;
using Platform.Application.Catalog.Variants.Commands;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Catalog.Variants;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Catalog.Variants;

public sealed class InMemoryVariantAdminApplicationService : IVariantAdminApplicationService
{
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IProductStatusDefinitionRepository _productStatusDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InMemoryVariantAdminApplicationService(
        IVariantRepository variantRepository,
        IProductRepository productRepository,
        IMediaAssetRepository mediaAssetRepository,
        IProductStatusDefinitionRepository productStatusDefinitionRepository,
        IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _productStatusDefinitionRepository = productStatusDefinitionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<VariantSummaryDto>> ListByProductAsync(ListVariantsByProductQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var variants = (await _variantRepository.ListByProductAsync(query.ProductId, cancellationToken))
            .OrderBy(x => x.Sku)
            .ToList();
        var items = await MapSummariesAsync(variants, cancellationToken);

        return items;
    }

    public async Task<IReadOnlyList<VariantLookupDto>> ListLookupsAsync(ListVariantLookupsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var variants = await _variantRepository.ListLookupsAsync(query, cancellationToken);
        var productIds = variants
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        var products = productIds.Count == 0
            ? []
            : await _productRepository.GetLookupByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(x => x.Id);

        return variants
            .Select(variant =>
            {
                var product = productMap.GetValueOrDefault(variant.ProductId);
                return new VariantLookupDto(
                    variant.Id,
                    variant.ProductId,
                    variant.Sku,
                    product?.ProductNumber ?? variant.ProductId.ToString(),
                    product?.Translations.FirstOrDefault()?.Name);
            })
            .OrderBy(x => x.ProductNumber)
            .ThenBy(x => x.Sku)
            .ToList();
    }

    public async Task<VariantDetailsDto?> GetByIdAsync(GetVariantByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var variant = await _variantRepository.GetByIdAsync(query.VariantId, cancellationToken);
        return variant is null ? null : await MapDetailsAsync(variant, cancellationToken);
    }

    public async Task<VariantDetailsDto?> CreateAsync(CreateVariantCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await _productRepository.GetByIdAsync(command.ProductId, cancellationToken) is null)
        {
            return null;
        }

        if (await _variantRepository.GetBySkuAsync(command.Sku, cancellationToken) is not null)
        {
            throw new ConflictException("Variant SKU already exists.");
        }

        var now = DateTime.UtcNow;
        var variant = new Variant(
            Guid.NewGuid(),
            command.ProductId,
            command.Sku,
            command.Ean,
            command.Mpn,
            command.Barcode,
            await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken),
            command.IsDefaultVariant,
            command.Weight,
            command.Length,
            command.Width,
            command.Height,
            now,
            now,
            command.AttributeValues.Select(MapAttributeValue));

        await _variantRepository.AddAsync(variant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(variant, cancellationToken);
    }

    public async Task<VariantDetailsDto?> UpdateAsync(UpdateVariantCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        var existingBySku = await _variantRepository.GetBySkuAsync(command.Sku, cancellationToken);
        if (existingBySku is not null && existingBySku.Id != command.VariantId)
        {
            throw new ConflictException("Variant SKU already exists.");
        }

        variant.Update(
            command.Sku,
            command.Ean,
            command.Mpn,
            command.Barcode,
            await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken),
            command.IsDefaultVariant,
            command.Weight,
            command.Length,
            command.Width,
            command.Height,
            command.AttributeValues.Select(MapAttributeValue),
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(variant, cancellationToken);
    }

    public async Task<VariantDetailsDto?> AssignStatusAsync(AssignVariantStatusCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        variant.AssignStatus(await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(variant, cancellationToken);
    }

    public async Task<VariantDetailsDto?> UpsertMediaAsync(UpsertVariantMediaCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        await ValidateMediaAssetAsync(command.MediaAssetId, command.Type, cancellationToken);
        variant.UpsertMedia(command.MediaAssetId, command.Type, command.SortOrder, command.IsPrimary, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(variant, cancellationToken);
    }

    public async Task<VariantDetailsDto?> RemoveMediaAsync(RemoveVariantMediaCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        variant.RemoveMedia(command.VariantMediaId, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(variant, cancellationToken);
    }

    private async Task<ProductStatusDefinition> ResolveStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var status = await _productStatusDefinitionRepository.GetByIdAsync(
            id,
            ProductStatusEntityType.Variant,
            cancellationToken);
        if (status is not null)
        {
            return status;
        }

        throw new RequestValidationException(nameof(CreateVariantCommand.ProductStatusDefinitionId), "Unknown variant status.");
    }

    private static VariantAttributeValue MapAttributeValue(CreateVariantAttributeValueCommand command)
    {
        return new VariantAttributeValue(Guid.NewGuid(), command.ProductAttributeId, command.AttributeOptionId, command.ValueText);
    }

    private async Task<IReadOnlyList<VariantSummaryDto>> MapSummariesAsync(IReadOnlyList<Variant> variants, CancellationToken cancellationToken)
    {
        var mediaAssetIds = variants.SelectMany(x => x.Media).Select(x => x.MediaAssetId).Distinct().ToList();
        var mediaAssets = mediaAssetIds.Count == 0
            ? []
            : await _mediaAssetRepository.GetByIdsAsync(mediaAssetIds, cancellationToken);
        var mediaAssetMap = mediaAssets.ToDictionary(x => x.Id);

        return variants
            .Select(variant => new VariantSummaryDto(
                variant.Id,
                variant.ProductId,
                variant.Sku,
                variant.Ean,
                variant.Mpn,
                variant.Barcode,
                variant.Status,
                MapStatus(variant.ProductStatus),
                variant.IsDefaultVariant,
                ResolvePrimaryImageUrl(variant, mediaAssetMap),
                variant.CreatedAtUtc,
                variant.UpdatedAtUtc,
                variant.RowVersion))
            .ToList();
    }

    private async Task<VariantDetailsDto> MapDetailsAsync(Variant variant, CancellationToken cancellationToken)
    {
        var mediaAssetIds = variant.Media.Select(x => x.MediaAssetId).Distinct().ToList();
        var mediaAssets = mediaAssetIds.Count == 0
            ? []
            : await _mediaAssetRepository.GetByIdsAsync(mediaAssetIds, cancellationToken);
        var mediaAssetMap = mediaAssets.ToDictionary(x => x.Id);

        return new VariantDetailsDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Ean,
            variant.Mpn,
            variant.Barcode,
            variant.Status,
            MapStatus(variant.ProductStatus),
            variant.IsDefaultVariant,
            ResolvePrimaryImageUrl(variant, mediaAssetMap),
            variant.Weight,
            variant.Length,
            variant.Width,
            variant.Height,
            variant.AttributeValues.Select(x => new VariantAttributeValueDto(x.ProductAttributeId, x.AttributeOptionId, x.ValueText)).ToList(),
            variant.Media.Select(media =>
            {
                var asset = mediaAssetMap.GetValueOrDefault(media.MediaAssetId);
                return new VariantMediaDto(
                    media.Id,
                    media.MediaAssetId,
                    media.Type,
                    media.SortOrder,
                    media.IsPrimary,
                    asset?.FileName ?? media.MediaAssetId.ToString(),
                    asset?.PublicUrl ?? string.Empty,
                    asset?.Title,
                    asset?.AltText);
            }).ToList(),
            variant.CreatedAtUtc,
            variant.UpdatedAtUtc,
            variant.RowVersion);
    }

    private async Task ValidateMediaAssetAsync(Guid mediaAssetId, string type, CancellationToken cancellationToken)
    {
        if (await _mediaAssetRepository.GetByIdAsync(mediaAssetId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertVariantMediaCommand.MediaAssetId), "Unknown media asset.");
        }

        if (!AllowedMediaTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(UpsertVariantMediaCommand.Type), "Unknown media type.");
        }
    }

    private static string? ResolvePrimaryImageUrl(Variant variant, IReadOnlyDictionary<Guid, Platform.Domain.Catalog.Media.MediaAsset> mediaAssetMap)
    {
        return variant.Media
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => mediaAssetMap.GetValueOrDefault(x.MediaAssetId)?.PublicUrl)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static ProductStatusDto MapStatus(ProductStatusDefinition status)
    {
        return new ProductStatusDto(status.Id, status.Code, status.Name, status.IsBuyable);
    }

    private static readonly string[] AllowedMediaTypes = ["Image", "Document"];
}
