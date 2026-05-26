using System.Net;
using System.Net.Http.Json;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;

namespace Platform.Backoffice.Integration;

public sealed partial class AdminApiClient
{
    public Task<PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>> ListStorefrontProjectionRefreshMessagesAsync(
        string? status,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var path = BuildStorefrontProjectionRefreshMessagesPath(status, sort, page, pageSize);
        return GetRequiredAsync<PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>>(path, cancellationToken);
    }

    public async Task<StorefrontProjectionRefreshMessageDetailsDto?> ResetStorefrontProjectionRefreshMessageAsync(
        Guid id,
        ResetStorefrontProjectionRefreshMessageRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"api/admin/storefront-projection-refresh-messages/{id}/reset",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<StorefrontProjectionRefreshMessageDetailsDto>(response, cancellationToken);
    }

    private static string BuildStorefrontProjectionRefreshMessagesPath(string? status, string? sort, int page, int pageSize)
    {
        var query = new List<string>();
        AddQuery(query, "status", status);
        AddQuery(query, "sort", sort);
        AddQuery(query, "page", page.ToString());
        AddQuery(query, "pageSize", pageSize.ToString());
        return query.Count == 0
            ? "api/admin/storefront-projection-refresh-messages"
            : $"api/admin/storefront-projection-refresh-messages?{string.Join("&", query)}";
    }
}
