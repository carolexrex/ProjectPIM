using Platform.Application.Catalog.Markets.Queries;
using Platform.Domain.Catalog.Markets;

namespace Platform.Application.Catalog.Markets;

public interface IMarketRepository
{
    Task<MarketListResult> ListAsync(ListMarketsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Market>> ListLookupsAsync(ListMarketLookupsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Market>> ListActiveAsync(CancellationToken cancellationToken);
    Task<Market?> GetByIdAsync(Guid marketId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Market>> GetByIdsAsync(IReadOnlyCollection<Guid> marketIds, CancellationToken cancellationToken);
    Task<Market?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Market market, CancellationToken cancellationToken);
}
