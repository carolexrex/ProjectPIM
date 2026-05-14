using Platform.Application.Catalog.Attributes.Queries;
using Platform.Domain.Catalog.Attributes;

namespace Platform.Application.Catalog.Attributes;

public interface IProductAttributeRepository
{
    Task<ProductAttributeListResult> ListAsync(ListProductAttributesQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductAttribute>> ListEditorDefinitionsAsync(ListProductAttributeEditorDefinitionsQuery query, CancellationToken cancellationToken);
    Task<ProductAttribute?> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductAttribute>> GetByIdsAsync(IReadOnlyCollection<Guid> attributeIds, CancellationToken cancellationToken);
    Task<ProductAttribute?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken);
}
