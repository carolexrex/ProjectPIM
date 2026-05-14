using Platform.Contracts.Storefront;

namespace Platform.Application.Storefront;

public interface IStorefrontContextApplicationService
{
    Task<StorefrontContextResolutionResult> GetContextAsync(GetStorefrontContextQuery query, CancellationToken cancellationToken);
}
