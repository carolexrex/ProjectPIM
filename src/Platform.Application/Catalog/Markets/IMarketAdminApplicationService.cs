using Platform.Application.Catalog.Markets.Commands;
using Platform.Application.Catalog.Markets.Queries;
using Platform.Contracts.Catalog.Markets;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Markets;

public interface IMarketAdminApplicationService
{
    Task<PagedResponse<MarketSummaryDto>> ListAsync(ListMarketsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketLookupDto>> ListLookupsAsync(ListMarketLookupsQuery query, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> GetByIdAsync(GetMarketByIdQuery query, CancellationToken cancellationToken);
    Task<MarketDetailsDto> CreateAsync(CreateMarketCommand command, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> UpdateAsync(UpdateMarketCommand command, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> ArchiveAsync(ArchiveMarketCommand command, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> AssignCurrenciesAsync(AssignMarketCurrenciesCommand command, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> AssignCulturesAsync(AssignMarketCulturesCommand command, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> UpsertProductAssignmentAsync(UpsertMarketProductAssignmentCommand command, CancellationToken cancellationToken);
    Task<MarketDetailsDto?> RemoveProductAssignmentAsync(RemoveMarketProductAssignmentCommand command, CancellationToken cancellationToken);
}
