using System.Net;
using System.Net.Http.Json;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Common;

namespace Platform.Backoffice.Integration;

public sealed partial class AdminApiClient
{
    public async Task<PagedResponse<InventoryLocationSummaryDto>> ListInventoryLocationsAsync(
        string? search,
        string? status,
        Guid? marketId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildInventoryLocationsPath(search, status, marketId, sort);
        return await GetRequiredAsync<PagedResponse<InventoryLocationSummaryDto>>(path, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> GetInventoryLocationAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/inventory-locations/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryLocationDetailsDto>(response, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto> CreateInventoryLocationAsync(CreateInventoryLocationRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/inventory-locations", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryLocationDetailsDto>(response, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> UpdateInventoryLocationAsync(Guid id, UpdateInventoryLocationRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/inventory-locations/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryLocationDetailsDto>(response, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> ArchiveInventoryLocationAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"api/admin/inventory-locations/{id}/archive", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryLocationDetailsDto>(response, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> UpsertInventoryLocationMarketAssignmentAsync(Guid id, UpsertInventoryLocationMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/inventory-locations/{id}/markets", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryLocationDetailsDto>(response, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> RemoveInventoryLocationMarketAssignmentAsync(Guid id, Guid marketId, RemoveInventoryLocationMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/inventory-locations/{id}/markets/{marketId}/remove", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryLocationDetailsDto>(response, cancellationToken);
    }

    public async Task<InventoryBalanceDto> UpsertInventoryBalanceAsync(UpsertInventoryBalanceRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync("api/admin/inventory-balances", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryBalanceDto>(response, cancellationToken);
    }

    public async Task<InventoryTransactionDto> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/inventory-transactions", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<InventoryTransactionDto>(response, cancellationToken);
    }

    public async Task<VariantInventorySnapshotDto?> GetVariantInventorySnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/variants/{id}/inventory", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<VariantInventorySnapshotDto>(response, cancellationToken);
    }

    private static string BuildInventoryLocationsPath(string? search, string? status, Guid? marketId, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "marketId", marketId?.ToString());
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/inventory-locations" : $"api/admin/inventory-locations?{string.Join("&", query)}";
    }
}
