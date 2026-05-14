using System.Text.Json;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Products.Commands;
using Platform.Application.Catalog.Products.Queries;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Integrations;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Domain.Catalog.Brands;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class InMemoryProductAdminApplicationService : IProductAdminApplicationService
{
    private readonly IProductRepository _productRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductAttributeRepository _productAttributeRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IProductStatusDefinitionRepository _productStatusDefinitionRepository;
    private readonly IOutboxEventPublisher _outboxEventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public InMemoryProductAdminApplicationService(
        IProductRepository productRepository,
        IBrandRepository brandRepository,
        ICategoryRepository categoryRepository,
        IProductAttributeRepository productAttributeRepository,
        IMediaAssetRepository mediaAssetRepository,
        IProductStatusDefinitionRepository productStatusDefinitionRepository,
        IOutboxEventPublisher outboxEventPublisher,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _productAttributeRepository = productAttributeRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _productStatusDefinitionRepository = productStatusDefinitionRepository;
        _outboxEventPublisher = outboxEventPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ProductSummaryDto>> ListAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _productRepository.ListAsync(query, cancellationToken);
        var brandIds = result.Items
            .Where(x => x.BrandId is not null)
            .Select(x => x.BrandId!.Value)
            .Distinct()
            .ToList();
        var brands = brandIds.Count == 0
            ? []
            : await _brandRepository.GetByIdsAsync(brandIds, cancellationToken);
        var brandMap = brands.ToDictionary(x => x.Id);
        var mediaAssetIds = result.Items
            .SelectMany(x => x.Media)
            .Select(x => x.MediaAssetId)
            .Distinct()
            .ToList();
        var mediaAssets = mediaAssetIds.Count == 0
            ? []
            : await _mediaAssetRepository.GetByIdsAsync(mediaAssetIds, cancellationToken);
        var mediaAssetMap = mediaAssets.ToDictionary(x => x.Id);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var items = result.Items
            .Select(product => MapSummary(product, brandMap, mediaAssetMap))
            .ToList();

        return new PagedResponse<ProductSummaryDto>(items, result.Total, page, pageSize);
    }

    public async Task<IReadOnlyList<ProductLookupDto>> ListLookupsAsync(ListProductLookupsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var products = await _productRepository.ListLookupsAsync(query, cancellationToken);
        return products
            .Select(product => new ProductLookupDto(
                product.Id,
                product.ProductNumber,
                product.Translations.FirstOrDefault()?.Name,
                product.HasVariants))
            .ToList();
    }

    public async Task<ProductDetailsDto?> GetByIdAsync(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        return product is null ? null : await MapDetailsAsync(product, cancellationToken);
    }

    public async Task<ProductDetailsDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await _productRepository.GetByProductNumberAsync(command.ProductNumber, cancellationToken) is not null)
        {
            throw new ConflictException("Product number already exists.");
        }

        await ValidateBrandAssignmentAsync(command.BrandId, cancellationToken);
        await ValidateCategoryIdsAsync(command.CategoryIds, cancellationToken);
        await ValidateAttributeValuesAsync(command.AttributeValues, cancellationToken);

        var status = await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken);
        var now = DateTime.UtcNow;
        var product = new Product(
            Guid.NewGuid(),
            command.ProductType,
            command.ProductNumber,
            command.Slug,
            command.BrandId,
            status,
            command.TaxCategoryCode,
            command.UnitOfMeasure,
            command.HasVariants,
            command.CategoryIds,
            command.AttributeValues.Select(MapAttributeValue),
            command.Weight,
            command.Length,
            command.Width,
            command.Height,
            now,
            now);

        await _productRepository.AddAsync(product, cancellationToken);
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductCreated, "Created", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        await ValidateBrandAssignmentAsync(command.BrandId, cancellationToken);
        await ValidateCategoryIdsAsync(command.CategoryIds, cancellationToken);
        await ValidateAttributeValuesAsync(command.AttributeValues, cancellationToken);

        product.Update(
            command.ProductType,
            command.Slug,
            command.BrandId,
            await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken),
            command.TaxCategoryCode,
            command.UnitOfMeasure,
            command.CategoryIds,
            command.AttributeValues.Select(MapAttributeValue),
            command.Weight,
            command.Length,
            command.Width,
            command.Height,
            command.RowVersion);

        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "Updated", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> ArchiveAsync(ArchiveProductCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Archive();
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "Archived", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> AssignStatusAsync(AssignProductStatusCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.AssignStatus(await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken));
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "StatusAssigned", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> UpsertMediaAsync(UpsertProductMediaCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        await ValidateMediaAssetAsync(command.MediaAssetId, command.Type, cancellationToken);

        product.UpsertMedia(command.MediaAssetId, command.Type, command.SortOrder, command.IsPrimary, command.RowVersion);
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "MediaUpserted", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> RemoveMediaAsync(RemoveProductMediaCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.RemoveMedia(command.ProductMediaId, command.RowVersion);
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "MediaRemoved", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> UpsertRelationAsync(UpsertProductRelationCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        await ValidateRelationAsync(command.ProductId, command.TargetProductId, command.RelationType, command.Quantity, cancellationToken);

        product.UpsertRelation(command.TargetProductId, command.RelationType, command.Quantity, command.SortOrder, command.RowVersion);
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "RelationUpserted", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductDetailsDto?> RemoveRelationAsync(RemoveProductRelationCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.RemoveRelation(command.RelationId, command.RowVersion);
        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "RelationRemoved", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<ProductTranslationDto?> UpsertTranslationAsync(UpsertProductTranslationCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var translation = product.UpsertTranslation(
            command.CultureCode,
            command.Name,
            command.ShortDescription,
            command.LongDescription,
            command.SeoTitle,
            command.SeoDescription);

        var details = await MapDetailsAsync(product, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.ProductUpdated, "TranslationUpserted", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTranslation(translation);
    }

    private Task PublishEventAsync(
        string eventType,
        string changeType,
        ProductDetailsDto details,
        CancellationToken cancellationToken)
    {
        var payload = new ProductWebhookEventDto(DateTime.UtcNow, changeType, details);
        return _outboxEventPublisher.EnqueueAsync(
            eventType,
            "Product",
            details.Id,
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }

    private async Task<ProductStatusDefinition> ResolveStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var status = await _productStatusDefinitionRepository.GetByIdAsync(
            id,
            ProductStatusEntityType.Product,
            cancellationToken);
        if (status is not null)
        {
            return status;
        }

        throw new RequestValidationException(nameof(CreateProductCommand.ProductStatusDefinitionId), "Unknown product status.");
    }

    private async Task ValidateCategoryIdsAsync(IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken)
    {
        var distinctCategoryIds = categoryIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctCategoryIds.Count == 0)
        {
            return;
        }

        var categories = await _categoryRepository.GetByIdsAsync(distinctCategoryIds, cancellationToken);
        if (categories.Count == distinctCategoryIds.Count)
        {
            return;
        }

        throw new RequestValidationException(nameof(CreateProductCommand.CategoryIds), "One or more categories do not exist.");
    }

    private async Task ValidateAttributeValuesAsync(
        IReadOnlyList<CreateProductAttributeValueCommand> attributeValues,
        CancellationToken cancellationToken)
    {
        var distinctAttributeIds = attributeValues
            .Select(x => x.ProductAttributeId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctAttributeIds.Count != attributeValues.Count)
        {
            throw new RequestValidationException(nameof(CreateProductCommand.AttributeValues), "Duplicate product attribute values are not allowed.");
        }

        if (distinctAttributeIds.Count == 0)
        {
            return;
        }

        var attributes = await _productAttributeRepository.GetByIdsAsync(distinctAttributeIds, cancellationToken);
        if (attributes.Count != distinctAttributeIds.Count)
        {
            throw new RequestValidationException(nameof(CreateProductCommand.AttributeValues), "One or more product attributes do not exist.");
        }

        foreach (var attributeValue in attributeValues)
        {
            var attribute = attributes.First(x => x.Id == attributeValue.ProductAttributeId);

            if (!string.Equals(attribute.Scope, "Product", StringComparison.OrdinalIgnoreCase))
            {
                throw new RequestValidationException(nameof(CreateProductCommand.AttributeValues), "Only product-scoped attributes can be assigned to products.");
            }

            var hasValue = attributeValue.AttributeOptionId is not null || !string.IsNullOrWhiteSpace(attributeValue.ValueText);
            if (attribute.IsRequired && !hasValue)
            {
                throw new RequestValidationException(nameof(CreateProductCommand.AttributeValues), $"Attribute '{attribute.Code}' requires a value.");
            }

            if (attributeValue.AttributeOptionId is Guid optionId)
            {
                if (!attribute.Options.Any(option => option.Id == optionId))
                {
                    throw new RequestValidationException(nameof(CreateProductCommand.AttributeValues), $"Option '{optionId}' does not belong to attribute '{attribute.Code}'.");
                }
            }
        }
    }

    private static ProductAttributeValue MapAttributeValue(CreateProductAttributeValueCommand command)
    {
        return new ProductAttributeValue(Guid.NewGuid(), command.ProductAttributeId, command.AttributeOptionId, command.ValueText);
    }

    private static ProductSummaryDto MapSummary(
        Product product,
        IReadOnlyDictionary<Guid, Brand> brandMap,
        IReadOnlyDictionary<Guid, Domain.Catalog.Media.MediaAsset> mediaAssetMap)
    {
        var defaultTranslation = product.Translations.FirstOrDefault();
        var brand = product.BrandId is Guid brandId ? brandMap.GetValueOrDefault(brandId) : null;
        var primaryImageUrl = product.Media
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => mediaAssetMap.GetValueOrDefault(x.MediaAssetId)?.PublicUrl)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return new ProductSummaryDto(
            product.Id,
            product.ProductNumber,
            product.Slug,
            product.ProductType,
            product.Status,
            MapStatus(product.ProductStatus),
            BrandName: ResolveBrandName(brand),
            DefaultName: defaultTranslation?.Name,
            PrimaryImageUrl: primaryImageUrl,
            product.HasVariants,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.RowVersion);
    }

    private async Task<ProductDetailsDto> MapDetailsAsync(Product product, CancellationToken cancellationToken)
    {
        var brand = product.BrandId is Guid brandId
            ? await _brandRepository.GetByIdAsync(brandId, cancellationToken)
            : null;
        var categoryIds = product.CategoryAssignments.Select(x => x.CategoryId).ToList();
        var categories = categoryIds.Count == 0
            ? []
            : await _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);
        var mediaAssetIds = product.Media.Select(x => x.MediaAssetId).Distinct().ToList();
        var mediaAssets = mediaAssetIds.Count == 0
            ? []
            : await _mediaAssetRepository.GetByIdsAsync(mediaAssetIds, cancellationToken);
        var mediaAssetMap = mediaAssets.ToDictionary(x => x.Id);
        var relationTargetIds = product.Relations.Select(x => x.TargetProductId).Distinct().ToList();
        var relationTargets = relationTargetIds.Count == 0
            ? []
            : await _productRepository.GetByIdsAsync(relationTargetIds, cancellationToken);
        var relationTargetMap = relationTargets.ToDictionary(x => x.Id);
        var primaryImageUrl = product.Media
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => mediaAssetMap.GetValueOrDefault(x.MediaAssetId)?.PublicUrl)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return new ProductDetailsDto(
            product.Id,
            product.ProductNumber,
            product.Slug,
            product.ProductType,
            product.Status,
            MapStatus(product.ProductStatus),
            product.BrandId,
            BrandName: ResolveBrandName(brand),
            product.TaxCategoryCode,
            product.UnitOfMeasure,
            primaryImageUrl,
            product.HasVariants,
            product.Weight,
            product.Length,
            product.Width,
            product.Height,
            categories
                .Select(category => new ProductCategoryAssignmentDto(
                    category.Id,
                    category.Code,
                    category.Translations.FirstOrDefault()?.Name))
                .OrderBy(x => x.Name ?? x.Code)
                .ToList(),
            product.AttributeValues
                .Select(x => new ProductAttributeValueDto(x.ProductAttributeId, x.AttributeOptionId, x.ValueText))
                .ToList(),
            product.Media
                .Select(media =>
                {
                    var asset = mediaAssetMap.GetValueOrDefault(media.MediaAssetId);
                    return new ProductMediaDto(
                        media.Id,
                        media.MediaAssetId,
                        media.Type,
                        media.SortOrder,
                        media.IsPrimary,
                        asset?.FileName ?? media.MediaAssetId.ToString(),
                        asset?.PublicUrl ?? string.Empty,
                        asset?.Title,
                        asset?.AltText);
                })
                .ToList(),
            product.Relations
                .Select(relation =>
                {
                    var target = relationTargetMap.GetValueOrDefault(relation.TargetProductId);
                    return new ProductRelationDto(
                        relation.Id,
                        relation.TargetProductId,
                        target?.ProductNumber ?? relation.TargetProductId.ToString(),
                        target?.Translations.FirstOrDefault()?.Name,
                        relation.RelationType,
                        relation.Quantity,
                        relation.SortOrder);
                })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.RelationType)
                .ThenBy(x => x.TargetProductNumber)
                .ToList(),
            product.Translations.Select(MapTranslation).ToList(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.RowVersion);
    }

    private async Task ValidateMediaAssetAsync(Guid mediaAssetId, string type, CancellationToken cancellationToken)
    {
        if (await _mediaAssetRepository.GetByIdAsync(mediaAssetId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertProductMediaCommand.MediaAssetId), "Unknown media asset.");
        }

        if (!AllowedMediaTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(UpsertProductMediaCommand.Type), "Unknown media type.");
        }
    }

    private async Task ValidateBrandAssignmentAsync(Guid? brandId, CancellationToken cancellationToken)
    {
        if (brandId is null)
        {
            return;
        }

        var brand = await _brandRepository.GetByIdAsync(brandId.Value, cancellationToken);
        if (brand is null)
        {
            throw new RequestValidationException(nameof(CreateProductCommand.BrandId), "Unknown brand.");
        }

        if (!string.Equals(brand.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(CreateProductCommand.BrandId), "Archived brands cannot be assigned to products.");
        }
    }

    private async Task ValidateRelationAsync(
        Guid productId,
        Guid targetProductId,
        string relationType,
        decimal? quantity,
        CancellationToken cancellationToken)
    {
        if (targetProductId == productId)
        {
            throw new RequestValidationException(nameof(UpsertProductRelationCommand.TargetProductId), "A product cannot relate to itself.");
        }

        if (await _productRepository.GetByIdAsync(targetProductId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertProductRelationCommand.TargetProductId), "Target product does not exist.");
        }

        var normalizedRelationType = relationType.Trim();
        if (!AllowedRelationTypes.Contains(normalizedRelationType, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(UpsertProductRelationCommand.RelationType), "Unknown relation type.");
        }

        if (string.Equals(normalizedRelationType, "BundleComponent", StringComparison.OrdinalIgnoreCase))
        {
            if (quantity is null || quantity <= 0)
            {
                throw new RequestValidationException(nameof(UpsertProductRelationCommand.Quantity), "Bundle components require quantity greater than zero.");
            }
        }
        else if (quantity is not null && quantity <= 0)
        {
            throw new RequestValidationException(nameof(UpsertProductRelationCommand.Quantity), "Quantity must be greater than zero when provided.");
        }
    }

    private static readonly string[] AllowedRelationTypes = ["RelatedProduct", "Accessory", "BundleComponent"];

    private static ProductStatusDto MapStatus(ProductStatusDefinition status)
    {
        return new ProductStatusDto(status.Id, status.Code, status.Name, status.IsBuyable);
    }

    private static ProductTranslationDto MapTranslation(ProductTranslation translation)
    {
        return new ProductTranslationDto(
            translation.CultureCode,
            translation.Name,
            translation.ShortDescription,
            translation.LongDescription,
            translation.SeoTitle,
            translation.SeoDescription);
    }

    private static string? ResolveBrandName(Brand? brand)
    {
        if (brand is null)
        {
            return null;
        }

        return brand.Translations.FirstOrDefault()?.Name ?? brand.Code;
    }

    private static readonly string[] AllowedMediaTypes = ["Image", "Document"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
