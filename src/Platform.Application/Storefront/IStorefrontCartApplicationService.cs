namespace Platform.Application.Storefront;

public interface IStorefrontCartApplicationService
{
    Task<StorefrontCartResult> CreateAsync(CreateStorefrontCartCommand command, CancellationToken cancellationToken);
    Task<StorefrontCartResult> GetByIdAsync(GetStorefrontCartByIdQuery query, CancellationToken cancellationToken);
    Task<StorefrontCartResult> RepriceAsync(RepriceStorefrontCartCommand command, CancellationToken cancellationToken);
    Task<StorefrontCheckoutResult> CheckoutAsync(CheckoutStorefrontCartCommand command, CancellationToken cancellationToken);
}
