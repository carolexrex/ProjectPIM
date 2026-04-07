using Platform.Domain.Catalog.Products;

namespace Platform.Application.Catalog.Products;

public interface IProductStatusDefinitionRepository
{
    Task<ProductStatusDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
