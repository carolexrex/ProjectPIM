using Platform.Application.Catalog.Brands.Queries;
using Platform.Domain.Catalog.Brands;

namespace Platform.Application.Catalog.Brands;

public interface IBrandRepository
{
    Task<BrandListResult> ListAsync(ListBrandsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Brand>> ListForExportAsync(string? search, string? status, CancellationToken cancellationToken);
    Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Brand>> GetByIdsAsync(IReadOnlyCollection<Guid> brandIds, CancellationToken cancellationToken);
    Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Brand brand, CancellationToken cancellationToken);
}
