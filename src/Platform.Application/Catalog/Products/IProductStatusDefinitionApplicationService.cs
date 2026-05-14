using Platform.Contracts.Catalog.Products;

namespace Platform.Application.Catalog.Products;

public interface IProductStatusDefinitionApplicationService
{
    Task<IReadOnlyList<ProductStatusDto>> ListAsync(string entityType, CancellationToken cancellationToken);
}
