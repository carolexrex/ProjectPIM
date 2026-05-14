using Platform.Application.Catalog.Brands.Commands;
using Platform.Application.Catalog.Brands.Queries;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Brands;

public interface IBrandAdminApplicationService
{
    Task<PagedResponse<BrandSummaryDto>> ListAsync(ListBrandsQuery query, CancellationToken cancellationToken);
    Task<BrandDetailsDto?> GetByIdAsync(GetBrandByIdQuery query, CancellationToken cancellationToken);
    Task<BrandDetailsDto> CreateAsync(CreateBrandCommand command, CancellationToken cancellationToken);
    Task<BrandDetailsDto?> UpdateAsync(UpdateBrandCommand command, CancellationToken cancellationToken);
    Task<BrandDetailsDto?> ArchiveAsync(ArchiveBrandCommand command, CancellationToken cancellationToken);
    Task<BrandTranslationDto?> UpsertTranslationAsync(UpsertBrandTranslationCommand command, CancellationToken cancellationToken);
}
