using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Errors;
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
    private readonly IProductStatusDefinitionRepository _productStatusDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InMemoryVariantAdminApplicationService(
        IVariantRepository variantRepository,
        IProductRepository productRepository,
        IProductStatusDefinitionRepository productStatusDefinitionRepository,
        IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _productStatusDefinitionRepository = productStatusDefinitionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<VariantSummaryDto>> ListByProductAsync(ListVariantsByProductQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = (await _variantRepository.ListByProductAsync(query.ProductId, cancellationToken))
            .OrderBy(x => x.Sku)
            .Select(MapSummary)
            .ToList();

        return items;
    }

    public async Task<VariantDetailsDto?> GetByIdAsync(GetVariantByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var variant = await _variantRepository.GetByIdAsync(query.VariantId, cancellationToken);
        return variant is null ? null : MapDetails(variant);
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
        return MapDetails(variant);
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
        return MapDetails(variant);
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
        return MapDetails(variant);
    }

    private async Task<ProductStatusDefinition> ResolveStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var status = await _productStatusDefinitionRepository.GetByIdAsync(id, cancellationToken);
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

    private static VariantSummaryDto MapSummary(Variant variant)
    {
        return new VariantSummaryDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Ean,
            variant.Mpn,
            variant.Barcode,
            variant.Status,
            MapStatus(variant.ProductStatus),
            variant.IsDefaultVariant,
            variant.PrimaryImageUrl,
            variant.CreatedAtUtc,
            variant.UpdatedAtUtc,
            variant.RowVersion);
    }

    private static VariantDetailsDto MapDetails(Variant variant)
    {
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
            variant.PrimaryImageUrl,
            variant.Weight,
            variant.Length,
            variant.Width,
            variant.Height,
            variant.AttributeValues.Select(x => new VariantAttributeValueDto(x.ProductAttributeId, x.AttributeOptionId, x.ValueText)).ToList(),
            variant.CreatedAtUtc,
            variant.UpdatedAtUtc,
            variant.RowVersion);
    }

    private static ProductStatusDto MapStatus(ProductStatusDefinition status)
    {
        return new ProductStatusDto(status.Id, status.Code, status.Name, status.IsBuyable);
    }
}
