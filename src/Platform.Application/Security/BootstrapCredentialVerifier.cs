using System.Security.Cryptography;
using System.Text;

namespace Platform.Application.Security;

public static class BootstrapCredentialVerifier
{
    private const string Scheme = "PBKDF2";
    private const string Algorithm = "SHA256";
    private const int DefaultIterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static bool HasConfiguredSecret(string? password, string? passwordHash)
    {
        return !string.IsNullOrWhiteSpace(password) || !string.IsNullOrWhiteSpace(passwordHash);
    }

    public static bool Matches(string providedPassword, string? configuredPassword, string? configuredPasswordHash)
    {
        if (!string.IsNullOrWhiteSpace(configuredPasswordHash))
        {
            return VerifyHashedPassword(providedPassword, configuredPasswordHash);
        }

        return !string.IsNullOrWhiteSpace(configuredPassword)
            && string.Equals(configuredPassword, providedPassword, StringComparison.Ordinal);
    }

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join(
            '$',
            Scheme,
            Algorithm,
            DefaultIterations.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool VerifyHashedPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5
            || !string.Equals(parts[0], Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[1], Algorithm, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(parts[2], out var iterations)
            || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
