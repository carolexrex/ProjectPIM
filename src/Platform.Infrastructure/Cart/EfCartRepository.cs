using Microsoft.EntityFrameworkCore;
using Platform.Application.Cart;
using Platform.Application.Cart.Queries;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Cart;

public sealed class EfCartRepository : ICartRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfCartRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CartListResult> ListAsync(ListCartsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Carts
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => !query.CustomerId.HasValue || x.CustomerId == query.CustomerId.Value)
            .Where(x => !query.CompanyId.HasValue || x.CompanyId == query.CompanyId.Value)
            .Where(x => !query.MarketId.HasValue || x.MarketId == query.MarketId.Value)
            .Where(x => !query.CreatedFromUtc.HasValue || x.CreatedAtUtc >= query.CreatedFromUtc.Value)
            .Where(x => !query.CreatedToUtc.HasValue || x.CreatedAtUtc <= query.CreatedToUtc.Value);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Lines)
            .Include(x => x.Addresses)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CartListResult(items, total);
    }

    public async Task<Platform.Domain.Cart.Cart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken)
    {
        return await _dbContext.Carts
            .Include(x => x.Lines)
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Id == cartId, cancellationToken);
    }

    public async Task AddAsync(Platform.Domain.Cart.Cart cart, CancellationToken cancellationToken)
    {
        await _dbContext.Carts.AddAsync(cart, cancellationToken);
    }

    private static IQueryable<Platform.Domain.Cart.Cart> ApplySorting(IQueryable<Platform.Domain.Cart.Cart> carts, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-createdatutc" => carts.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "createdatutc" => carts.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "-grandtotal" => carts.OrderByDescending(x => x.GrandTotal).ThenBy(x => x.Id),
            "grandtotal" => carts.OrderBy(x => x.GrandTotal).ThenBy(x => x.Id),
            _ => carts.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
        };
    }
}
