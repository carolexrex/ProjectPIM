namespace Platform.Infrastructure.Storefront;

public interface IStorefrontProjectionChangeTracker
{
    void DiscardPendingChanges();
}
