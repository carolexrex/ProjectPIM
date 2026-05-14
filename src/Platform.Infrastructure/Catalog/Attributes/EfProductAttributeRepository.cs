using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Attributes.Queries;
using Platform.Domain.Catalog.Attributes;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Attributes;

public sealed class EfProductAttributeRepository : IProductAttributeRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfProductAttributeRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductAttributeListResult> ListAsync(ListProductAttributesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.ProductAttributes
            .AsNoTracking()
            .Where(attribute => string.IsNullOrWhiteSpace(query.Search)
                || attribute.Code.Contains(query.Search)
                || attribute.Name.Contains(query.Search))
            .Where(attribute => string.IsNullOrWhiteSpace(query.Status) || attribute.Status == query.Status)
            .Where(attribute => string.IsNullOrWhiteSpace(query.Scope) || attribute.Scope == query.Scope)
            .Where(attribute => string.IsNullOrWhiteSpace(query.DataType) || attribute.DataType == query.DataType);

        var total = await filteredQuery.CountAsync(cancellationToken);

        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Options)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ProductAttributeListResult(items, total);
    }

    public async Task<IReadOnlyList<ProductAttribute>> ListEditorDefinitionsAsync(ListProductAttributeEditorDefinitionsQuery query, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductAttributes
            .AsNoTracking()
            .Where(attribute => string.IsNullOrWhiteSpace(query.Status) || attribute.Status == query.Status)
            .Where(attribute => string.IsNullOrWhiteSpace(query.Scope) || attribute.Scope == query.Scope)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Include(x => x.Options)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductAttribute?> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductAttributes
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == attributeId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductAttribute>> GetByIdsAsync(IReadOnlyCollection<Guid> attributeIds, CancellationToken cancellationToken)
    {
        if (attributeIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.ProductAttributes
            .Include(x => x.Options)
            .Where(x => attributeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductAttribute?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductAttributes
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken)
    {
        await _dbContext.ProductAttributes.AddAsync(attribute, cancellationToken);
    }

    private static IQueryable<ProductAttribute> ApplySorting(IQueryable<ProductAttribute> attributes, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => attributes.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => attributes.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-sortorder" => attributes.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Code),
            "sortorder" => attributes.OrderBy(x => x.SortOrder).ThenBy(x => x.Code),
            "-code" => attributes.OrderByDescending(x => x.Code),
            _ => attributes.OrderBy(x => x.Code)
        };
    }
}
