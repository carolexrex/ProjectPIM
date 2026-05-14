using Platform.Application.Catalog.Pricing.Queries;
using Platform.Domain.Catalog.Pricing;

namespace Platform.Application.Catalog.Pricing;

public interface IPriceListRepository
{
    Task<PriceListListResult> ListAsync(ListPriceListsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceList>> ListActiveByMarketAsync(Guid marketId, string currencyCode, DateTime instantUtc, CancellationToken cancellationToken);
    Task<PriceList?> GetByIdAsync(Guid priceListId, CancellationToken cancellationToken);
    Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(PriceList priceList, CancellationToken cancellationToken);
}
