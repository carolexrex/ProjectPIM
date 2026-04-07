using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Products.Commands;
using Platform.Application.Catalog.Products.Queries;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Errors;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class InMemoryProductAdminApplicationService : IProductAdminApplicationService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusDefinitionRepository _productStatusDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InMemoryProductAdminApplicationService(
        IProductRepository productRepository,
        IProductStatusDefinitionRepository productStatusDefinitionRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _productStatusDefinitionRepository = productStatusDefinitionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ProductSummaryDto>> ListAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var products = await _productRepository.ListAsync(cancellationToken);

        var filtered = products
            .Where(product => string.IsNullOrWhiteSpace(query.Search)
                || product.ProductNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || product.Translations.Any(t => t.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(product => string.IsNullOrWhiteSpace(query.Status)
                || string.Equals(product.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(product => string.IsNullOrWhiteSpace(query.ProductStatusCode)
                || string.Equals(product.ProductStatus.Code, query.ProductStatusCode, StringComparison.OrdinalIgnoreCase))
            .Where(product => query.BrandId is null || product.BrandId == query.BrandId)
            .Where(product => query.HasVariants is null || product.HasVariants == query.HasVariants)
            .OrderBy(product => product.ProductNumber)
            .ToList();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapSummary)
            .ToList();

        return new PagedResponse<ProductSummaryDto>(items, filtered.Count, page, pageSize);
    }

    public async Task<ProductDetailsDto?> GetByIdAsync(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        return product is null ? null : MapDetails(product);
    }

    public async Task<ProductDetailsDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await _productRepository.GetByProductNumberAsync(command.ProductNumber, cancellationToken) is not null)
        {
            throw new ConflictException("Product number already exists.");
        }

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
            command.Weight,
            command.Length,
            command.Width,
            command.Height,
            now,
            now);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(product);
    }

    public async Task<ProductDetailsDto?> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Update(
            command.ProductType,
            command.Slug,
            command.BrandId,
            await ResolveStatusAsync(command.ProductStatusDefinitionId, cancellationToken),
            command.TaxCategoryCode,
            command.UnitOfMeasure,
            command.Weight,
            command.Length,
            command.Width,
            command.Height,
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(product);
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(product);
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(product);
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTranslation(translation);
    }

    private async Task<ProductStatusDefinition> ResolveStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var status = await _productStatusDefinitionRepository.GetByIdAsync(id, cancellationToken);
        if (status is not null)
        {
            return status;
        }

        throw new RequestValidationException(nameof(CreateProductCommand.ProductStatusDefinitionId), "Unknown product status.");
    }

    private static ProductSummaryDto MapSummary(Product product)
    {
        var defaultTranslation = product.Translations.FirstOrDefault();
        return new ProductSummaryDto(
            product.Id,
            product.ProductNumber,
            product.Slug,
            product.ProductType,
            product.Status,
            MapStatus(product.ProductStatus),
            BrandName: null,
            DefaultName: defaultTranslation?.Name,
            PrimaryImageUrl: product.PrimaryImageUrl,
            product.HasVariants,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.RowVersion);
    }

    private static ProductDetailsDto MapDetails(Product product)
    {
        return new ProductDetailsDto(
            product.Id,
            product.ProductNumber,
            product.Slug,
            product.ProductType,
            product.Status,
            MapStatus(product.ProductStatus),
            product.BrandId,
            BrandName: null,
            product.TaxCategoryCode,
            product.UnitOfMeasure,
            product.PrimaryImageUrl,
            product.HasVariants,
            product.Weight,
            product.Length,
            product.Width,
            product.Height,
            product.Translations.Select(MapTranslation).ToList(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.RowVersion);
    }

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
}
