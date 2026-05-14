using Platform.Application.Catalog.Attributes.Commands;
using Platform.Application.Catalog.Attributes.Queries;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Attributes;

public interface IProductAttributeAdminApplicationService
{
    Task<PagedResponse<ProductAttributeSummaryDto>> ListAsync(ListProductAttributesQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductAttributeEditorDefinitionDto>> ListEditorDefinitionsAsync(ListProductAttributeEditorDefinitionsQuery query, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto?> GetByIdAsync(GetProductAttributeByIdQuery query, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto> CreateAsync(CreateProductAttributeCommand command, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto?> UpdateAsync(UpdateProductAttributeCommand command, CancellationToken cancellationToken);
    Task<ProductAttributeDetailsDto?> ArchiveAsync(ArchiveProductAttributeCommand command, CancellationToken cancellationToken);
}
