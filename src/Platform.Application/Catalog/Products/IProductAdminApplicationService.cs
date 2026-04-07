using Platform.Application.Catalog.Products.Commands;
using Platform.Application.Catalog.Products.Queries;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Products;

public interface IProductAdminApplicationService
{
    Task<PagedResponse<ProductSummaryDto>> ListAsync(ListProductsQuery query, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> GetByIdAsync(GetProductByIdQuery query, CancellationToken cancellationToken);
    Task<ProductDetailsDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> ArchiveAsync(ArchiveProductCommand command, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> AssignStatusAsync(AssignProductStatusCommand command, CancellationToken cancellationToken);
    Task<ProductTranslationDto?> UpsertTranslationAsync(UpsertProductTranslationCommand command, CancellationToken cancellationToken);
}
