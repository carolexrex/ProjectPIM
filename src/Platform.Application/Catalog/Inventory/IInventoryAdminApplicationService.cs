using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Catalog.Inventory.Queries;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Inventory;

public interface IInventoryAdminApplicationService
{
    Task<PagedResponse<InventoryLocationSummaryDto>> ListLocationsAsync(ListInventoryLocationsQuery query, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> GetLocationByIdAsync(GetInventoryLocationByIdQuery query, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto> CreateLocationAsync(CreateInventoryLocationCommand command, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> UpdateLocationAsync(UpdateInventoryLocationCommand command, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> ArchiveLocationAsync(ArchiveInventoryLocationCommand command, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> UpsertLocationMarketAssignmentAsync(UpsertInventoryLocationMarketAssignmentCommand command, CancellationToken cancellationToken);
    Task<InventoryLocationDetailsDto?> RemoveLocationMarketAssignmentAsync(RemoveInventoryLocationMarketAssignmentCommand command, CancellationToken cancellationToken);
    Task<InventoryBalanceDto> UpsertBalanceAsync(UpsertInventoryBalanceCommand command, CancellationToken cancellationToken);
    Task<InventoryTransactionDto> AdjustInventoryAsync(AdjustInventoryCommand command, CancellationToken cancellationToken);
    Task<VariantInventorySnapshotDto?> GetVariantInventorySnapshotAsync(GetVariantInventorySnapshotQuery query, CancellationToken cancellationToken);
}
