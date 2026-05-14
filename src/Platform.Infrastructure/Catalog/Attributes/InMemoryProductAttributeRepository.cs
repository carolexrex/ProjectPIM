using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Attributes.Queries;
using Platform.Domain.Catalog.Attributes;

namespace Platform.Infrastructure.Catalog.Attributes;

public sealed class InMemoryProductAttributeRepository : IProductAttributeRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryProductAttributeRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<ProductAttributeListResult> ListAsync(ListProductAttributesQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.ProductAttributes.Values
                    .Where(attribute => string.IsNullOrWhiteSpace(query.Search)
                        || attribute.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                        || attribute.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    .Where(attribute => string.IsNullOrWhiteSpace(query.Status)
                        || string.Equals(attribute.Status, query.Status, StringComparison.OrdinalIgnoreCase))
                    .Where(attribute => string.IsNullOrWhiteSpace(query.Scope)
                        || string.Equals(attribute.Scope, query.Scope, StringComparison.OrdinalIgnoreCase))
                    .Where(attribute => string.IsNullOrWhiteSpace(query.DataType)
                        || string.Equals(attribute.DataType, query.DataType, StringComparison.OrdinalIgnoreCase)),
                query.Sort)
            .ToList();

        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new ProductAttributeListResult(items, filtered.Count));
    }

    public Task<IReadOnlyList<ProductAttribute>> ListEditorDefinitionsAsync(ListProductAttributeEditorDefinitionsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ProductAttribute> items = _store.ProductAttributes.Values
            .Where(attribute => string.IsNullOrWhiteSpace(query.Status)
                || string.Equals(attribute.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(attribute => string.IsNullOrWhiteSpace(query.Scope)
                || string.Equals(attribute.Scope, query.Scope, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<ProductAttribute?> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.ProductAttributes.TryGetValue(attributeId, out var attribute) ? attribute : null);
    }

    public Task<IReadOnlyList<ProductAttribute>> GetByIdsAsync(IReadOnlyCollection<Guid> attributeIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = attributeIds
            .Distinct()
            .Where(id => _store.ProductAttributes.ContainsKey(id))
            .Select(id => _store.ProductAttributes[id])
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductAttribute>>(items);
    }

    public Task<ProductAttribute?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var attribute = _store.ProductAttributes.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(attribute);
    }

    public Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.ProductAttributes[attribute.Id] = attribute;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<ProductAttribute> ApplySorting(IEnumerable<ProductAttribute> attributes, string? sort)
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
