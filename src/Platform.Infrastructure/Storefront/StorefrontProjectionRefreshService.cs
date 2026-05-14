using Platform.Application.Catalog.Products;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Storefront;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontProjectionRefreshService : IStorefrontProjectionRefreshService
{
    private readonly IStorefrontProjectionBuilder _builder;
    private readonly IStorefrontProductProjectionRepository _projectionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StorefrontProjectionRefreshService(
        IStorefrontProjectionBuilder builder,
        IStorefrontProductProjectionRepository projectionRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _builder = builder;
        _projectionRepository = projectionRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RefreshProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var projections = await _builder.BuildForProductAsync(productId, cancellationToken);
        await _projectionRepository.ReplaceForProductAsync(productId, projections, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        foreach (var productId in productIds.Distinct())
        {
            var projections = await _builder.BuildForProductAsync(productId, cancellationToken);
            await _projectionRepository.ReplaceForProductAsync(productId, projections, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RebuildAllAsync(CancellationToken cancellationToken)
    {
        var products = await _productRepository.ListForExportAsync(
            null,
            null,
            null,
            null,
            null,
            cancellationToken);

        await _projectionRepository.DeleteAllAsync(cancellationToken);

        foreach (var product in products)
        {
            var projections = await _builder.BuildForProductAsync(product.Id, cancellationToken);
            await _projectionRepository.ReplaceForProductAsync(product.Id, projections, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
