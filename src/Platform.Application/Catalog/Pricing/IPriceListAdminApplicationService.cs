using Platform.Application.Catalog.Pricing.Commands;
using Platform.Application.Catalog.Pricing.Queries;
using Platform.Contracts.Catalog.Pricing;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Pricing;

public interface IPriceListAdminApplicationService
{
    Task<PagedResponse<PriceListSummaryDto>> ListAsync(ListPriceListsQuery query, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> GetByIdAsync(GetPriceListByIdQuery query, CancellationToken cancellationToken);
    Task<PriceListDetailsDto> CreateAsync(CreatePriceListCommand command, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> UpdateAsync(UpdatePriceListCommand command, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> ArchiveAsync(ArchivePriceListCommand command, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> UpsertMarketAssignmentAsync(UpsertPriceListMarketAssignmentCommand command, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> RemoveMarketAssignmentAsync(RemovePriceListMarketAssignmentCommand command, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> UpsertEntryAsync(UpsertPriceListEntryCommand command, CancellationToken cancellationToken);
    Task<PriceListDetailsDto?> RemoveEntryAsync(RemovePriceListEntryCommand command, CancellationToken cancellationToken);
}
