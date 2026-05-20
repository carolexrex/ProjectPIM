namespace Platform.Infrastructure.Storefront;

public sealed class NoOpStorefrontProjectionChangeTracker : IStorefrontProjectionChangeTracker
{
    public void DiscardPendingChanges()
    {
    }
}
