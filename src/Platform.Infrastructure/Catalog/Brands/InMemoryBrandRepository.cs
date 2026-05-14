using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Brands.Queries;
using Platform.Domain.Catalog.Brands;

namespace Platform.Infrastructure.Catalog.Brands;

public sealed class InMemoryBrandRepository : IBrandRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryBrandRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<BrandListResult> ListAsync(ListBrandsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
            _store.Brands.Values
                .Where(brand => string.IsNullOrWhiteSpace(query.Search)
                    || brand.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                    || brand.Translations.Any(x => x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                .Where(brand => string.IsNullOrWhiteSpace(query.Status)
                    || string.Equals(brand.Status, query.Status, StringComparison.OrdinalIgnoreCase)),
            query.Sort)
            .ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new BrandListResult(items, filtered.Count));
    }

    public Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Brands.TryGetValue(brandId, out var brand) ? brand : null);
    }

    public Task<IReadOnlyList<Brand>> ListForExportAsync(string? search, string? status, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Brand> items = ApplySorting(
                _store.Brands.Values
                    .Where(brand => string.IsNullOrWhiteSpace(search)
                        || brand.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || brand.Translations.Any(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .Where(brand => string.IsNullOrWhiteSpace(status)
                        || string.Equals(brand.Status, status, StringComparison.OrdinalIgnoreCase)),
                "code")
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Brand>> GetByIdsAsync(IReadOnlyCollection<Guid> brandIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Brand> items = brandIds
            .Where(id => _store.Brands.ContainsKey(id))
            .Select(id => _store.Brands[id])
            .ToList();

        return Task.FromResult(items);
    }

    public Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var brand = _store.Brands.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(brand);
    }

    public Task AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Brands[brand.Id] = brand;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<Brand> ApplySorting(IEnumerable<Brand> brands, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => brands.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => brands.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-sortorder" => brands.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Code),
            "sortorder" => brands.OrderBy(x => x.SortOrder).ThenBy(x => x.Code),
            "-code" => brands.OrderByDescending(x => x.Code),
            _ => brands.OrderBy(x => x.Code)
        };
    }
}
