using Platform.Domain.Catalog.Products;

namespace Platform.Application.Catalog.Products;

public interface IProductStatusDefinitionRepository
{
    Task<IReadOnlyList<ProductStatusDefinition>> ListAsync(
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken);

    Task<ProductStatusDefinition?> GetByIdAsync(
        Guid id,
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken);

    Task<ProductStatusDefinition?> GetByCodeAsync(
        string code,
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken);
}
