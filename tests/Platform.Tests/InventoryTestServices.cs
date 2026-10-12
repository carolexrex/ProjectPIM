using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// In-memory inventory reservation wiring shared by tests that construct order services by hand.
/// </summary>
internal static class InventoryTestServices
{
    public static InventoryReservationService CreateReservationService(InMemoryCatalogStore store, TimeProvider? timeProvider = null)
    {
        return new InventoryReservationService(
            new InMemoryInventoryLocationRepository(store),
            new InMemoryInventoryBalanceRepository(store),
            new InMemoryInventoryAllocationRepository(store),
            new InMemoryMarketRepository(store),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store))),
            timeProvider ?? TimeProvider.System);
    }

    public static InMemoryInventoryWriteCoordinator CreateCoordinator(InMemoryCatalogStore store)
    {
        return new InMemoryInventoryWriteCoordinator(store);
    }
}
