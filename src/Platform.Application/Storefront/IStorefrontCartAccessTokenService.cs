using CartDomain = Platform.Domain.Cart;

namespace Platform.Application.Storefront;

/// <summary>
/// Issues and validates storefront cart access tokens.
/// </summary>
/// <remarks>
/// The token is a <em>bearer capability</em>, not an identity or session. It proves ownership of a
/// single cart and carries no customer/shopper identity. Anyone holding it can read, reprice, and
/// check out that cart until the cart itself expires, so clients must keep it confidential and the
/// API must only return it over the cart owner's own response. It is the only ownership proof:
/// <c>rowVersion</c> is optimistic concurrency control, not a secret.
/// </remarks>
public interface IStorefrontCartAccessTokenService
{
    string CreateToken(CartDomain.Cart cart);
    bool IsValid(CartDomain.Cart cart, string? token);
}
