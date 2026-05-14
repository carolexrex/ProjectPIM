using Platform.Application.Catalog.Products;
using Platform.Application.Abstractions.Errors;
using Platform.Contracts.Catalog.Products;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class ProductStatusDefinitionApplicationService : IProductStatusDefinitionApplicationService
{
    private readonly IProductStatusDefinitionRepository _repository;

    public ProductStatusDefinitionApplicationService(IProductStatusDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductStatusDto>> ListAsync(string entityType, CancellationToken cancellationToken)
    {
        var parsedEntityType = entityType.Trim().ToLowerInvariant() switch
        {
            "product" => ProductStatusEntityType.Product,
            "variant" => ProductStatusEntityType.Variant,
            _ => throw new RequestValidationException(nameof(entityType), "Expected 'product' or 'variant'.")
        };

        var statuses = await _repository.ListAsync(parsedEntityType, cancellationToken);

        return statuses
            .Select(x => new ProductStatusDto(x.Id, x.Code, x.Name, x.IsBuyable))
            .ToList();
    }
}
