using Microsoft.Extensions.Options;
using Platform.Infrastructure.Storefront;
using CartDomain = Platform.Domain.Cart;

namespace Platform.Tests;

public sealed class StorefrontCartAccessTokenServiceTests
{
    [Fact]
    public void Token_RoundTripsAcrossServicesWithSameSigningKey()
    {
        var cart = CreateCart();
        var firstService = CreateService("shared-storefront-cart-access-token-signing-key");
        var secondService = CreateService("shared-storefront-cart-access-token-signing-key");

        var token = firstService.CreateToken(cart);

        Assert.True(secondService.IsValid(cart, token));
    }

    [Fact]
    public void Token_IsRejectedWithDifferentSigningKey()
    {
        var cart = CreateCart();
        var firstService = CreateService("first-storefront-cart-access-token-signing-key");
        var secondService = CreateService("second-storefront-cart-access-token-signing-key");

        var token = firstService.CreateToken(cart);

        Assert.False(secondService.IsValid(cart, token));
    }

    [Fact]
    public void Token_DoesNotDependOnTimestampPrecision()
    {
        var service = CreateService("shared-storefront-cart-access-token-signing-key");
        var cart = CreateCart(new DateTime(2026, 5, 27, 12, 0, 0, 123, DateTimeKind.Utc).AddTicks(4567));
        var reloadedCart = CreateCart(new DateTime(2026, 5, 27, 12, 0, 0, 123, DateTimeKind.Utc).AddTicks(4000));

        var token = service.CreateToken(cart);

        Assert.True(service.IsValid(reloadedCart, token));
    }

    private static StorefrontCartAccessTokenService CreateService(string signingKey)
    {
        return new StorefrontCartAccessTokenService(Options.Create(new StorefrontCartAccessTokenOptions
        {
            SigningKey = signingKey
        }));
    }

    private static CartDomain.Cart CreateCart()
    {
        return CreateCart(new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc));
    }

    private static CartDomain.Cart CreateCart(DateTime createdAtUtc)
    {
        return new CartDomain.Cart(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            customerId: null,
            companyId: null,
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            "SEK",
            "sv-SE",
            "buyer@example.com",
            createdAtUtc.AddDays(30),
            createdAtUtc,
            createdAtUtc);
    }
}
