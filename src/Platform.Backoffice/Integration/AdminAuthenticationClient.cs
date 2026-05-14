using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Platform.Contracts.Security;

namespace Platform.Backoffice.Integration;

public sealed class AdminAuthenticationClient : IAdminAuthenticationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AdminAuthenticationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AdminLoginResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/admin/auth/login",
            new AdminLoginRequest(username, password),
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var message = $"Admin API authentication failed with status {(int)response.StatusCode}.";
            throw new AdminApiException(message, (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<AdminLoginResponse>(JsonOptions, cancellationToken);
        if (result is null)
        {
            throw new AdminApiException("The admin API returned an empty authentication response.", (int)response.StatusCode);
        }

        return result;
    }
}
