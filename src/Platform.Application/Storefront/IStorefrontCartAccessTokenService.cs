using CartDomain = Platform.Domain.Cart;

namespace Platform.Application.Storefront;

public interface IStorefrontCartAccessTokenService
{
    string CreateToken(CartDomain.Cart cart);
    bool IsValid(CartDomain.Cart cart, string? token);
}
