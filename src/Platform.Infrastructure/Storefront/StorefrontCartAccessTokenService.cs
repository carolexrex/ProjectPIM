using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Platform.Application.Storefront;
using CartDomain = Platform.Domain.Cart;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontCartAccessTokenService : IStorefrontCartAccessTokenService
{
    private const string TokenPrefix = "sfct_v1";
    private static readonly Lazy<byte[]> EphemeralSigningKey = new(() => RandomNumberGenerator.GetBytes(64));

    private readonly byte[] _signingKey;

    public StorefrontCartAccessTokenService(IOptions<StorefrontCartAccessTokenOptions> options)
    {
        _signingKey = string.IsNullOrWhiteSpace(options.Value.SigningKey)
            ? EphemeralSigningKey.Value
            : Encoding.UTF8.GetBytes(options.Value.SigningKey.Trim());
    }

    public string CreateToken(CartDomain.Cart cart)
    {
        var cartId = cart.Id.ToString("N");
        var marketId = cart.MarketId.ToString("N");
        var payload = $"{cartId}.{marketId}";
        var signature = Sign(payload);
        return $"{TokenPrefix}.{payload}.{Base64UrlEncode(signature)}";
    }

    public bool IsValid(CartDomain.Cart cart, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!Guid.TryParse(parts[1], out var tokenCartId) || tokenCartId != cart.Id)
        {
            return false;
        }

        if (!Guid.TryParse(parts[2], out var tokenMarketId) || tokenMarketId != cart.MarketId)
        {
            return false;
        }

        var payload = $"{parts[1]}.{parts[2]}";
        if (!TryBase64UrlDecode(parts[3], out var suppliedSignature))
        {
            return false;
        }

        var expectedSignature = Sign(payload);
        return CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature);
    }

    private byte[] Sign(string payload)
    {
        using var hmac = new HMACSHA256(_signingKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryBase64UrlDecode(string value, out byte[] decoded)
    {
        decoded = [];
        try
        {
            var padded = value
                .Replace('-', '+')
                .Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
            decoded = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
