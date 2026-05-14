using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Products;
using Platform.Application.Storefront;
using Platform.Application.Integrations;
using Platform.Domain.Catalog.Attributes;
using Platform.Domain.Catalog.Categories;
using Platform.Domain.Common;
using Platform.Domain.Catalog.Media;
using Platform.Domain.Catalog.Brands;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class IntegrationJobExecutionService : IIntegrationJobExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IIntegrationJobRepository _integrationJobRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductAttributeRepository _productAttributeRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusDefinitionRepository _productStatusDefinitionRepository;
    private readonly IStorefrontProductProjectionRepository _storefrontProductProjectionRepository;
    private readonly IStorefrontProjectionRefreshService _storefrontProjectionRefreshService;
    private readonly IOutboxEventPublisher _outboxEventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IntegrationJobExecutionService> _logger;

    public IntegrationJobExecutionService(
        IIntegrationJobRepository integrationJobRepository,
        IBrandRepository brandRepository,
        ICategoryRepository categoryRepository,
        IProductAttributeRepository productAttributeRepository,
        IMediaAssetRepository mediaAssetRepository,
        IProductRepository productRepository,
        IProductStatusDefinitionRepository productStatusDefinitionRepository,
        IStorefrontProductProjectionRepository storefrontProductProjectionRepository,
        IStorefrontProjectionRefreshService storefrontProjectionRefreshService,
        IOutboxEventPublisher outboxEventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<IntegrationJobExecutionService> logger)
    {
        _integrationJobRepository = integrationJobRepository;
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _productAttributeRepository = productAttributeRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _productRepository = productRepository;
        _productStatusDefinitionRepository = productStatusDefinitionRepository;
        _storefrontProductProjectionRepository = storefrontProductProjectionRepository;
        _storefrontProjectionRefreshService = storefrontProjectionRefreshService;
        _outboxEventPublisher = outboxEventPublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExecutePendingAsync(int maxJobs, CancellationToken cancellationToken)
    {
        if (maxJobs <= 0)
        {
            return 0;
        }

        var executed = 0;

        for (var i = 0; i < maxJobs; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var job = await _integrationJobRepository.GetNextRunnableAsync(DateTime.UtcNow, cancellationToken);
            if (job is null)
            {
                break;
            }

            try
            {
                job.Start(job.RowVersion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                continue;
            }

            try
            {
                await ExecuteCoreAsync(job, cancellationToken);
                await PublishIntegrationJobEventAsync(WebhookEventTypes.IntegrationJobCompleted, job, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                executed++;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Integration job {JobId} failed.", job.Id);

                job.Fail(exception.Message, DateTime.UtcNow.AddMinutes(1), job.RowVersion);
                await PublishIntegrationJobEventAsync(WebhookEventTypes.IntegrationJobFailed, job, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                executed++;
            }
        }

        return executed;
    }

    private async Task ExecuteCoreAsync(IntegrationJob job, CancellationToken cancellationToken)
    {
        switch (job.Type)
        {
            case IntegrationJobTypes.BrandExport:
                await ExecuteBrandExportAsync(job, cancellationToken);
                return;
            case IntegrationJobTypes.BrandImport:
                await ExecuteBrandImportAsync(job, cancellationToken);
                return;
            case IntegrationJobTypes.ProductExport:
                await ExecuteProductExportAsync(job, cancellationToken);
                return;
            case IntegrationJobTypes.ProductImport:
                await ExecuteProductImportAsync(job, cancellationToken);
                return;
            case IntegrationJobTypes.StorefrontProjectionRebuild:
                await ExecuteStorefrontProjectionRebuildAsync(job, cancellationToken);
                return;
            default:
                throw new InvalidOperationException($"Unsupported integration job type '{job.Type}'.");
        }
    }

    private async Task ExecuteStorefrontProjectionRebuildAsync(IntegrationJob job, CancellationToken cancellationToken)
    {
        await _storefrontProjectionRefreshService.RebuildAllAsync(cancellationToken);

        var products = await _productRepository.ListForExportAsync(
            null,
            null,
            null,
            null,
            null,
            cancellationToken);

        var projectionCount = 0;
        foreach (var product in products)
        {
            projectionCount += (await _storefrontProductProjectionRepository.ListByProductIdAsync(product.Id, cancellationToken)).Count;
        }

        var result = new StorefrontProjectionRebuildJobResult(DateTime.UtcNow, projectionCount);
        job.Complete(
            $"Rebuilt {result.ProjectionCount} storefront product projections.",
            JsonSerializer.Serialize(result, JsonOptions),
            job.RowVersion);
    }

    private async Task ExecuteBrandExportAsync(IntegrationJob job, CancellationToken cancellationToken)
    {
        var payload = string.IsNullOrWhiteSpace(job.PayloadJson)
            ? new BrandExportJobPayload(null, null)
            : JsonSerializer.Deserialize<BrandExportJobPayload>(job.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Integration job payload could not be deserialized.");

        var brands = await _brandRepository.ListForExportAsync(payload.Search, payload.Status, cancellationToken);
        var exportedAtUtc = DateTime.UtcNow;

        var result = new BrandExportJobResult(
            exportedAtUtc,
            brands.Count,
            brands.Select(brand => new BrandExportJobResultItem(
                brand.Id,
                brand.Code,
                brand.Translations.FirstOrDefault()?.Name,
                brand.Status,
                brand.SortOrder,
                brand.UpdatedAtUtc)).ToList());

        job.Complete(
            $"Exported {result.TotalCount} brands.",
            JsonSerializer.Serialize(result, JsonOptions),
            job.RowVersion);
    }

    private async Task ExecuteBrandImportAsync(IntegrationJob job, CancellationToken cancellationToken)
    {
        var payload = string.IsNullOrWhiteSpace(job.PayloadJson)
            ? throw new InvalidOperationException("Integration job payload is required.")
            : JsonSerializer.Deserialize<BrandImportJobPayload>(job.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Integration job payload could not be deserialized.");

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultItems = new List<BrandImportJobResultItem>(payload.Brands.Count);
        var createdCount = 0;
        var updatedCount = 0;
        var failedCount = 0;

        for (var index = 0; index < payload.Brands.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = payload.Brands[index];
            var rowNumber = index + 1;
            var validationError = await ValidateBrandImportItemAsync(item, seenCodes, cancellationToken);

            if (validationError is not null)
            {
                failedCount++;
                resultItems.Add(new BrandImportJobResultItem(rowNumber, NormalizeRequired(item.Code), "Failed", validationError));
                continue;
            }

            var normalizedCode = NormalizeRequired(item.Code);
            var existing = await _brandRepository.GetByCodeAsync(normalizedCode, cancellationToken);
            var isCreated = existing is null;
            var brand = existing ?? new Brand(
                Guid.NewGuid(),
                normalizedCode,
                NormalizeOptional(item.WebsiteUrl),
                item.LogoMediaAssetId,
                item.SortOrder,
                DateTime.UtcNow,
                DateTime.UtcNow);

            if (!isCreated)
            {
                brand.Update(
                    normalizedCode,
                    NormalizeOptional(item.WebsiteUrl),
                    item.LogoMediaAssetId,
                    item.SortOrder,
                    brand.RowVersion);
            }

            foreach (var translation in item.Translations)
            {
                brand.UpsertTranslation(
                    NormalizeRequired(translation.CultureCode),
                    NormalizeRequired(translation.Name),
                    NormalizeRequired(translation.Slug),
                    NormalizeOptional(translation.Description));
            }

            if (isCreated)
            {
                await _brandRepository.AddAsync(brand, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (isCreated)
            {
                createdCount++;
                resultItems.Add(new BrandImportJobResultItem(rowNumber, normalizedCode, "Created", null));
            }
            else
            {
                updatedCount++;
                resultItems.Add(new BrandImportJobResultItem(rowNumber, normalizedCode, "Updated", null));
            }
        }

        var result = new BrandImportJobResult(
            DateTime.UtcNow,
            payload.Brands.Count,
            createdCount,
            updatedCount,
            failedCount,
            resultItems);

        job.Complete(
            $"Imported {result.TotalCount} brands: {result.CreatedCount} created, {result.UpdatedCount} updated, {result.FailedCount} failed.",
            JsonSerializer.Serialize(result, JsonOptions),
            job.RowVersion);
    }

    private async Task<string?> ValidateBrandImportItemAsync(
        BrandImportJobPayloadItem item,
        ISet<string> seenCodes,
        CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(item.Code);
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Code is required.";
        }

        if (code.Length > 64)
        {
            return "Code exceeds the maximum length of 64 characters.";
        }

        if (!seenCodes.Add(code))
        {
            return "Duplicate brand code within the same import job.";
        }

        if (!string.IsNullOrWhiteSpace(item.WebsiteUrl) && item.WebsiteUrl.Trim().Length > 1024)
        {
            return "WebsiteUrl exceeds the maximum length of 1024 characters.";
        }

        if (item.SortOrder < 0)
        {
            return "SortOrder must be zero or greater.";
        }

        if (item.LogoMediaAssetId.HasValue && await _mediaAssetRepository.GetByIdAsync(item.LogoMediaAssetId.Value, cancellationToken) is null)
        {
            return "LogoMediaAssetId references an unknown media asset.";
        }

        var cultureCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var translation in item.Translations)
        {
            var cultureCode = NormalizeRequired(translation.CultureCode);
            var name = NormalizeRequired(translation.Name);
            var slug = NormalizeRequired(translation.Slug);

            if (string.IsNullOrWhiteSpace(cultureCode) || cultureCode.Length is < 2 or > 16)
            {
                return "Translation culture code must be between 2 and 16 characters.";
            }

            if (!cultureCodes.Add(cultureCode))
            {
                return $"Duplicate translation culture '{cultureCode}'.";
            }

            if (string.IsNullOrWhiteSpace(name) || name.Length > 256)
            {
                return "Translation name is required and must not exceed 256 characters.";
            }

            if (string.IsNullOrWhiteSpace(slug) || slug.Length > 256)
            {
                return "Translation slug is required and must not exceed 256 characters.";
            }
        }

        return null;
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task PublishIntegrationJobEventAsync(string eventType, IntegrationJob job, CancellationToken cancellationToken)
    {
        var payload = new IntegrationJobWebhookEventPayload(
            job.Id,
            job.Type,
            job.Direction,
            job.Status,
            job.RequestedBy,
            job.AttemptCount,
            job.ResultSummary,
            job.LastError,
            job.StartedAtUtc,
            job.CompletedAtUtc);

        await _outboxEventPublisher.EnqueueAsync(
            eventType,
            nameof(IntegrationJob),
            job.Id,
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }

    private async Task ExecuteProductExportAsync(IntegrationJob job, CancellationToken cancellationToken)
    {
        var payload = string.IsNullOrWhiteSpace(job.PayloadJson)
            ? new ProductExportJobPayload(null, null, null, null, null)
            : JsonSerializer.Deserialize<ProductExportJobPayload>(job.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Integration job payload could not be deserialized.");

        var products = await _productRepository.ListForExportAsync(
            payload.Search,
            payload.Status,
            payload.ProductStatusCode,
            payload.BrandId,
            payload.HasVariants,
            cancellationToken);

        var brandIds = products
            .Where(x => x.BrandId is not null)
            .Select(x => x.BrandId!.Value)
            .Distinct()
            .ToList();
        var categoryIds = products
            .SelectMany(x => x.CategoryAssignments)
            .Select(x => x.CategoryId)
            .Distinct()
            .ToList();
        var attributeIds = products
            .SelectMany(x => x.AttributeValues)
            .Select(x => x.ProductAttributeId)
            .Distinct()
            .ToList();
        var mediaAssetIds = products
            .SelectMany(x => x.Media)
            .Select(x => x.MediaAssetId)
            .Distinct()
            .ToList();
        var relationTargetIds = products
            .SelectMany(x => x.Relations)
            .Select(x => x.TargetProductId)
            .Distinct()
            .ToList();

        var brands = brandIds.Count == 0 ? [] : await _brandRepository.GetByIdsAsync(brandIds, cancellationToken);
        var categories = categoryIds.Count == 0 ? [] : await _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);
        var attributes = attributeIds.Count == 0 ? [] : await _productAttributeRepository.GetByIdsAsync(attributeIds, cancellationToken);
        var mediaAssets = mediaAssetIds.Count == 0 ? [] : await _mediaAssetRepository.GetByIdsAsync(mediaAssetIds, cancellationToken);
        var relationTargets = relationTargetIds.Count == 0 ? [] : await _productRepository.GetByIdsAsync(relationTargetIds, cancellationToken);

        var brandMap = brands.ToDictionary(x => x.Id);
        var categoryMap = categories.ToDictionary(x => x.Id);
        var attributeMap = attributes.ToDictionary(x => x.Id);
        var mediaAssetMap = mediaAssets.ToDictionary(x => x.Id);
        var relationTargetMap = relationTargets.ToDictionary(x => x.Id);

        var result = new ProductExportJobResult(
            DateTime.UtcNow,
            products.Count,
            products.Select(product => MapProductExportItem(
                product,
                brandMap,
                categoryMap,
                attributeMap,
                mediaAssetMap,
                relationTargetMap))
            .ToList());

        job.Complete(
            $"Exported {result.TotalCount} products.",
            JsonSerializer.Serialize(result, JsonOptions),
            job.RowVersion);
    }

    private static ProductExportJobResultItem MapProductExportItem(
        Product product,
        IReadOnlyDictionary<Guid, Brand> brandMap,
        IReadOnlyDictionary<Guid, Category> categoryMap,
        IReadOnlyDictionary<Guid, ProductAttribute> attributeMap,
        IReadOnlyDictionary<Guid, MediaAsset> mediaAssetMap,
        IReadOnlyDictionary<Guid, Product> relationTargetMap)
    {
        var brand = product.BrandId is Guid brandId ? brandMap.GetValueOrDefault(brandId) : null;

        return new ProductExportJobResultItem(
            product.Id,
            product.ProductNumber,
            product.Slug,
            product.ProductType,
            product.Status,
            new ProductExportJobStatusResult(
                product.ProductStatus.Id,
                product.ProductStatus.Code,
                product.ProductStatus.Name,
                product.ProductStatus.IsBuyable),
            product.BrandId,
            brand?.Code,
            ResolveBrandName(brand),
            product.HasVariants,
            product.TaxCategoryCode,
            product.UnitOfMeasure,
            product.Weight,
            product.Length,
            product.Width,
            product.Height,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.Translations
                .Select(translation => new ProductExportJobTranslationResult(
                    translation.CultureCode,
                    translation.Name,
                    translation.ShortDescription,
                    translation.LongDescription,
                    translation.SeoTitle,
                    translation.SeoDescription))
                .OrderBy(x => x.CultureCode, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            product.CategoryAssignments
                .Select(assignment =>
                {
                    var category = categoryMap.GetValueOrDefault(assignment.CategoryId);
                    return new ProductExportJobCategoryResult(
                        assignment.CategoryId,
                        category?.Code ?? assignment.CategoryId.ToString(),
                        category?.Translations.FirstOrDefault()?.Name);
                })
                .OrderBy(x => x.Name ?? x.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            product.AttributeValues
                .Select(value =>
                {
                    var attribute = attributeMap.GetValueOrDefault(value.ProductAttributeId);
                    var optionCode = value.AttributeOptionId is Guid optionId
                        ? attribute?.Options.FirstOrDefault(x => x.Id == optionId)?.Code
                        : null;

                    return new ProductExportJobAttributeValueResult(
                        value.ProductAttributeId,
                        attribute?.Code,
                        value.AttributeOptionId,
                        optionCode,
                        value.ValueText);
                })
                .OrderBy(x => x.ProductAttributeCode ?? x.ProductAttributeId.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToList(),
            product.Media
                .Select(media =>
                {
                    var asset = mediaAssetMap.GetValueOrDefault(media.MediaAssetId);
                    return new ProductExportJobMediaResult(
                        media.Id,
                        media.MediaAssetId,
                        media.Type,
                        media.SortOrder,
                        media.IsPrimary,
                        asset?.FileName,
                        asset?.PublicUrl,
                        asset?.Title,
                        asset?.AltText);
                })
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            product.Relations
                .Select(relation =>
                {
                    var target = relationTargetMap.GetValueOrDefault(relation.TargetProductId);
                    return new ProductExportJobRelationResult(
                        relation.Id,
                        relation.TargetProductId,
                        target?.ProductNumber,
                        target?.Translations.FirstOrDefault()?.Name,
                        relation.RelationType,
                        relation.Quantity,
                        relation.SortOrder);
                })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.RelationType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.TargetProductNumber ?? x.TargetProductId.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static string? ResolveBrandName(Brand? brand)
    {
        return brand?.Translations.FirstOrDefault()?.Name ?? brand?.Code;
    }

    private async Task ExecuteProductImportAsync(IntegrationJob job, CancellationToken cancellationToken)
    {
        var payload = string.IsNullOrWhiteSpace(job.PayloadJson)
            ? throw new InvalidOperationException("Integration job payload is required.")
            : JsonSerializer.Deserialize<ProductImportJobPayload>(job.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Integration job payload could not be deserialized.");

        var seenProductNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultItems = new List<ProductImportJobResultItem>(payload.Products.Count);
        var createdCount = 0;
        var updatedCount = 0;
        var failedCount = 0;

        for (var index = 0; index < payload.Products.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = payload.Products[index];
            var rowNumber = index + 1;

            var validation = await ValidateProductImportItemAsync(row, seenProductNumbers, seenSlugs, cancellationToken);
            if (validation.Error is not null)
            {
                failedCount++;
                resultItems.Add(new ProductImportJobResultItem(rowNumber, NormalizeRequired(row.ProductNumber), "Failed", validation.Error));
                continue;
            }

            var existing = await _productRepository.GetByProductNumberAsync(validation.ProductNumber!, cancellationToken);
            var isCreated = existing is null;
            Product product;

            if (isCreated)
            {
                product = new Product(
                    Guid.NewGuid(),
                    validation.ProductType!,
                    validation.ProductNumber!,
                    validation.Slug!,
                    validation.Brand?.Id,
                    validation.ProductStatus!,
                    validation.TaxCategoryCode!,
                    validation.UnitOfMeasure!,
                    row.HasVariants,
                    validation.Categories.Select(x => x.Id),
                    validation.AttributeValues,
                    row.Weight,
                    row.Length,
                    row.Width,
                    row.Height,
                    DateTime.UtcNow,
                    DateTime.UtcNow);
            }
            else
            {
                product = existing!;
                if (product.HasVariants != row.HasVariants)
                {
                    failedCount++;
                    resultItems.Add(new ProductImportJobResultItem(
                        rowNumber,
                        validation.ProductNumber!,
                        "Failed",
                        "HasVariants cannot be changed for an existing product."));
                    continue;
                }

                product.Update(
                    validation.ProductType!,
                    validation.Slug!,
                    validation.Brand?.Id,
                    validation.ProductStatus!,
                    validation.TaxCategoryCode!,
                    validation.UnitOfMeasure!,
                    validation.Categories.Select(x => x.Id),
                    validation.AttributeValues,
                    row.Weight,
                    row.Length,
                    row.Width,
                    row.Height,
                    product.RowVersion);
            }

            foreach (var translation in row.Translations)
            {
                product.UpsertTranslation(
                    NormalizeRequired(translation.CultureCode),
                    NormalizeRequired(translation.Name),
                    NormalizeOptional(translation.ShortDescription),
                    NormalizeOptional(translation.LongDescription),
                    NormalizeOptional(translation.SeoTitle),
                    NormalizeOptional(translation.SeoDescription));
            }

            if (isCreated)
            {
                await _productRepository.AddAsync(product, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (isCreated)
            {
                createdCount++;
                resultItems.Add(new ProductImportJobResultItem(rowNumber, validation.ProductNumber!, "Created", null));
            }
            else
            {
                updatedCount++;
                resultItems.Add(new ProductImportJobResultItem(rowNumber, validation.ProductNumber!, "Updated", null));
            }
        }

        var result = new ProductImportJobResult(
            DateTime.UtcNow,
            payload.Products.Count,
            createdCount,
            updatedCount,
            failedCount,
            resultItems);

        job.Complete(
            $"Imported {result.TotalCount} products: {result.CreatedCount} created, {result.UpdatedCount} updated, {result.FailedCount} failed.",
            JsonSerializer.Serialize(result, JsonOptions),
            job.RowVersion);
    }

    private async Task<ProductImportValidationResult> ValidateProductImportItemAsync(
        ProductImportJobPayloadItem row,
        ISet<string> seenProductNumbers,
        ISet<string> seenSlugs,
        CancellationToken cancellationToken)
    {
        var productType = NormalizeRequired(row.ProductType);
        var productNumber = NormalizeRequired(row.ProductNumber);
        var slug = NormalizeRequired(row.Slug);
        var productStatusCode = NormalizeRequired(row.ProductStatusCode);
        var taxCategoryCode = NormalizeRequired(row.TaxCategoryCode);
        var unitOfMeasure = NormalizeRequired(row.UnitOfMeasure);

        if (string.IsNullOrWhiteSpace(productType) || productType.Length > 64)
        {
            return ProductImportValidationResult.Failed("ProductType is required and must not exceed 64 characters.");
        }

        if (string.IsNullOrWhiteSpace(productNumber) || productNumber.Length > 64)
        {
            return ProductImportValidationResult.Failed("ProductNumber is required and must not exceed 64 characters.");
        }

        if (!seenProductNumbers.Add(productNumber))
        {
            return ProductImportValidationResult.Failed("Duplicate ProductNumber within the same import job.");
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 256)
        {
            return ProductImportValidationResult.Failed("Slug is required and must not exceed 256 characters.");
        }

        if (!seenSlugs.Add(slug))
        {
            return ProductImportValidationResult.Failed("Duplicate Slug within the same import job.");
        }

        if (string.IsNullOrWhiteSpace(productStatusCode) || productStatusCode.Length > 64)
        {
            return ProductImportValidationResult.Failed("ProductStatusCode is required and must not exceed 64 characters.");
        }

        if (string.IsNullOrWhiteSpace(taxCategoryCode) || taxCategoryCode.Length > 64)
        {
            return ProductImportValidationResult.Failed("TaxCategoryCode is required and must not exceed 64 characters.");
        }

        if (string.IsNullOrWhiteSpace(unitOfMeasure) || unitOfMeasure.Length > 32)
        {
            return ProductImportValidationResult.Failed("UnitOfMeasure is required and must not exceed 32 characters.");
        }

        var brand = await ResolveBrandByCodeAsync(row.BrandCode, cancellationToken);
        if (brand is null && !string.IsNullOrWhiteSpace(row.BrandCode))
        {
            return ProductImportValidationResult.Failed("BrandCode references an unknown or archived brand.");
        }

        var existingByProductNumber = await _productRepository.GetByProductNumberAsync(productNumber, cancellationToken);
        var existingBySlug = await _productRepository.GetBySlugAsync(slug, cancellationToken);
        if (existingBySlug is not null && existingBySlug.Id != existingByProductNumber?.Id)
        {
            return ProductImportValidationResult.Failed("Slug already exists on another product.");
        }

        var productStatus = await _productStatusDefinitionRepository.GetByCodeAsync(
            productStatusCode,
            ProductStatusEntityType.Product,
            cancellationToken);
        if (productStatus is null)
        {
            return ProductImportValidationResult.Failed("ProductStatusCode references an unknown product status.");
        }

        var categoryCodes = row.CategoryCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categories = new List<Category>(categoryCodes.Count);
        foreach (var categoryCode in categoryCodes)
        {
            var category = await _categoryRepository.GetByCodeAsync(categoryCode, cancellationToken);
            if (category is null)
            {
                return ProductImportValidationResult.Failed($"CategoryCode '{categoryCode}' does not exist.");
            }

            categories.Add(category);
        }

        var attributeValues = new List<ProductAttributeValue>(row.AttributeValues.Count);
        var seenAttributeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attributeValue in row.AttributeValues)
        {
            var attributeCode = NormalizeRequired(attributeValue.ProductAttributeCode);
            if (string.IsNullOrWhiteSpace(attributeCode) || attributeCode.Length > 64)
            {
                return ProductImportValidationResult.Failed("ProductAttributeCode is required and must not exceed 64 characters.");
            }

            if (!seenAttributeCodes.Add(attributeCode))
            {
                return ProductImportValidationResult.Failed($"Duplicate ProductAttributeCode '{attributeCode}' is not allowed.");
            }

            var attribute = await _productAttributeRepository.GetByCodeAsync(attributeCode, cancellationToken);
            if (attribute is null)
            {
                return ProductImportValidationResult.Failed($"ProductAttributeCode '{attributeCode}' does not exist.");
            }

            if (!string.Equals(attribute.Scope, "Product", StringComparison.OrdinalIgnoreCase))
            {
                return ProductImportValidationResult.Failed($"Attribute '{attributeCode}' is not product-scoped.");
            }

            var optionCode = NormalizeOptional(attributeValue.AttributeOptionCode);
            var option = optionCode is null
                ? null
                : attribute.Options.FirstOrDefault(x => string.Equals(x.Code, optionCode, StringComparison.OrdinalIgnoreCase));

            if (optionCode is not null && option is null)
            {
                return ProductImportValidationResult.Failed($"Attribute option '{optionCode}' does not belong to attribute '{attributeCode}'.");
            }

            var valueText = NormalizeOptional(attributeValue.ValueText);
            if (valueText is not null && valueText.Length > 256)
            {
                return ProductImportValidationResult.Failed($"ValueText for attribute '{attributeCode}' must not exceed 256 characters.");
            }

            var hasValue = option is not null || valueText is not null;
            if (attribute.IsRequired && !hasValue)
            {
                return ProductImportValidationResult.Failed($"Attribute '{attributeCode}' requires a value.");
            }

            attributeValues.Add(new ProductAttributeValue(Guid.NewGuid(), attribute.Id, option?.Id, valueText));
        }

        var seenCultureCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var translation in row.Translations)
        {
            var cultureCode = NormalizeRequired(translation.CultureCode);
            var name = NormalizeRequired(translation.Name);
            var seoTitle = NormalizeOptional(translation.SeoTitle);
            var seoDescription = NormalizeOptional(translation.SeoDescription);
            var shortDescription = NormalizeOptional(translation.ShortDescription);

            if (string.IsNullOrWhiteSpace(cultureCode) || cultureCode.Length is < 2 or > 16)
            {
                return ProductImportValidationResult.Failed("Translation CultureCode must be between 2 and 16 characters.");
            }

            if (!seenCultureCodes.Add(cultureCode))
            {
                return ProductImportValidationResult.Failed($"Duplicate translation culture '{cultureCode}'.");
            }

            if (string.IsNullOrWhiteSpace(name) || name.Length > 256)
            {
                return ProductImportValidationResult.Failed("Translation Name is required and must not exceed 256 characters.");
            }

            if (shortDescription is not null && shortDescription.Length > 1024)
            {
                return ProductImportValidationResult.Failed($"ShortDescription for culture '{cultureCode}' must not exceed 1024 characters.");
            }

            if (seoTitle is not null && seoTitle.Length > 256)
            {
                return ProductImportValidationResult.Failed($"SeoTitle for culture '{cultureCode}' must not exceed 256 characters.");
            }

            if (seoDescription is not null && seoDescription.Length > 512)
            {
                return ProductImportValidationResult.Failed($"SeoDescription for culture '{cultureCode}' must not exceed 512 characters.");
            }
        }

        return ProductImportValidationResult.Succeeded(
            productType,
            productNumber,
            slug,
            taxCategoryCode,
            unitOfMeasure,
            brand,
            productStatus,
            categories,
            attributeValues);
    }

    private async Task<Brand?> ResolveBrandByCodeAsync(string? brandCode, CancellationToken cancellationToken)
    {
        var normalizedBrandCode = NormalizeOptional(brandCode);
        if (normalizedBrandCode is null)
        {
            return null;
        }

        var brand = await _brandRepository.GetByCodeAsync(normalizedBrandCode, cancellationToken);
        return brand is not null && string.Equals(brand.Status, "Active", StringComparison.OrdinalIgnoreCase)
            ? brand
            : null;
    }

    private sealed record ProductImportValidationResult(
        string? Error,
        string? ProductType,
        string? ProductNumber,
        string? Slug,
        string? TaxCategoryCode,
        string? UnitOfMeasure,
        Brand? Brand,
        ProductStatusDefinition? ProductStatus,
        IReadOnlyList<Category> Categories,
        IReadOnlyList<ProductAttributeValue> AttributeValues)
    {
        public static ProductImportValidationResult Failed(string error)
        {
            return new ProductImportValidationResult(error, null, null, null, null, null, null, null, [], []);
        }

        public static ProductImportValidationResult Succeeded(
            string productType,
            string productNumber,
            string slug,
            string taxCategoryCode,
            string unitOfMeasure,
            Brand? brand,
            ProductStatusDefinition productStatus,
            IReadOnlyList<Category> categories,
            IReadOnlyList<ProductAttributeValue> attributeValues)
        {
            return new ProductImportValidationResult(
                null,
                productType,
                productNumber,
                slug,
                taxCategoryCode,
                unitOfMeasure,
                brand,
                productStatus,
                categories,
                attributeValues);
        }
    }
}
