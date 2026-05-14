using Platform.Application.Security;

namespace Platform.Tests;

public sealed class BootstrapCredentialVerifierTests
{
    [Fact]
    public void HashPassword_RoundTripsSuccessfully()
    {
        const string password = "dev-password-123";

        var hash = BootstrapCredentialVerifier.HashPassword(password);

        Assert.True(BootstrapCredentialVerifier.Matches(password, configuredPassword: null, configuredPasswordHash: hash));
        Assert.False(BootstrapCredentialVerifier.Matches("wrong-password", configuredPassword: null, configuredPasswordHash: hash));
    }

    [Fact]
    public void Matches_FallsBackToPlaintextConfiguration()
    {
        Assert.True(BootstrapCredentialVerifier.Matches("plain-secret", configuredPassword: "plain-secret", configuredPasswordHash: null));
        Assert.False(BootstrapCredentialVerifier.Matches("wrong-secret", configuredPassword: "plain-secret", configuredPasswordHash: null));
    }

    [Fact]
    public void VerifyHashedPassword_ReturnsFalseForMalformedHash()
    {
        Assert.False(BootstrapCredentialVerifier.VerifyHashedPassword("dev-password-123", "not-a-valid-hash"));
    }
}
