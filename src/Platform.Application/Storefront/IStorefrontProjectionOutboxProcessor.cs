namespace Platform.Application.Storefront;

public interface IStorefrontProjectionOutboxProcessor
{
    Task<int> ExecutePendingAsync(int maxMessages, CancellationToken cancellationToken);
}
