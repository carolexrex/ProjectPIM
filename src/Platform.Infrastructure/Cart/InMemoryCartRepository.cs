using Platform.Application.Cart;
using Platform.Application.Cart.Queries;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Cart;

public sealed class InMemoryCartRepository : ICartRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryCartRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<CartListResult> ListAsync(ListCartsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.Carts.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(x => !query.CustomerId.HasValue || x.CustomerId == query.CustomerId.Value)
            .Where(x => !query.CompanyId.HasValue || x.CompanyId == query.CompanyId.Value)
            .Where(x => !query.MarketId.HasValue || x.MarketId == query.MarketId.Value)
            .Where(x => !query.CreatedFromUtc.HasValue || x.CreatedAtUtc >= query.CreatedFromUtc.Value)
            .Where(x => !query.CreatedToUtc.HasValue || x.CreatedAtUtc <= query.CreatedToUtc.Value);

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "-createdatutc" => filtered.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "createdatutc" => filtered.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "-grandtotal" => filtered.OrderByDescending(x => x.GrandTotal).ThenBy(x => x.Id),
            "grandtotal" => filtered.OrderBy(x => x.GrandTotal).ThenBy(x => x.Id),
            _ => filtered.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new CartListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<Platform.Domain.Cart.Cart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken)
    {
        _store.Carts.TryGetValue(cartId, out var cart);
        return Task.FromResult(cart);
    }

    public Task AddAsync(Platform.Domain.Cart.Cart cart, CancellationToken cancellationToken)
    {
        _store.Carts[cart.Id] = cart;
        return Task.CompletedTask;
    }
}
