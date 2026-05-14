using System.Net;
using System.Net.Http.Json;
using Platform.Contracts.Cart;
using Platform.Contracts.Common;
using Platform.Contracts.Orders;

namespace Platform.Backoffice.Integration;

public sealed partial class AdminApiClient
{
    public async Task<PagedResponse<CartSummaryDto>> ListCartsAsync(
        string? status,
        Guid? customerId,
        Guid? companyId,
        Guid? marketId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildCartsPath(status, customerId, companyId, marketId, createdFromUtc, createdToUtc, sort);
        return await GetRequiredAsync<PagedResponse<CartSummaryDto>>(path, cancellationToken);
    }

    public async Task<CartDetailsDto?> GetCartAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/carts/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CartDetailsDto>(response, cancellationToken);
    }

    public async Task<CartDetailsDto?> RepriceCartAsync(Guid id, RepriceCartRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/carts/{id}/reprice", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CartDetailsDto>(response, cancellationToken);
    }

    public async Task<CartDetailsDto?> ExpireCartAsync(Guid id, ExpireCartRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/carts/{id}/expire", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CartDetailsDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<OrderSummaryDto>> ListOrdersAsync(
        string? status,
        string? paymentStatus,
        string? fulfillmentStatus,
        Guid? customerId,
        Guid? companyId,
        Guid? marketId,
        DateTime? placedFromUtc,
        DateTime? placedToUtc,
        string? search,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildOrdersPath(status, paymentStatus, fulfillmentStatus, customerId, companyId, marketId, placedFromUtc, placedToUtc, search, sort);
        return await GetRequiredAsync<PagedResponse<OrderSummaryDto>>(path, cancellationToken);
    }

    public async Task<OrderDetailsDto?> GetOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/orders/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<OrderDetailsDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderStatusHistoryDto>?> GetOrderStatusHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/orders/{id}/status-history", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<IReadOnlyList<OrderStatusHistoryDto>>(response, cancellationToken);
    }

    public async Task<OrderDetailsDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/orders", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<OrderDetailsDto>(response, cancellationToken);
    }

    public async Task<OrderStatusHistoryDto?> ChangeOrderStatusAsync(Guid id, ChangeOrderStatusRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/orders/{id}/status", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<OrderStatusHistoryDto>(response, cancellationToken);
    }

    public async Task<PaymentTransactionDto?> AddOrderPaymentTransactionAsync(Guid id, AddPaymentTransactionRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/orders/{id}/payment-transactions", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<PaymentTransactionDto>(response, cancellationToken);
    }

    private static string BuildCartsPath(
        string? status,
        Guid? customerId,
        Guid? companyId,
        Guid? marketId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "status", status);
        AddQuery(query, "customerId", customerId?.ToString());
        AddQuery(query, "companyId", companyId?.ToString());
        AddQuery(query, "marketId", marketId?.ToString());
        AddQuery(query, "createdFromUtc", createdFromUtc?.ToString("O"));
        AddQuery(query, "createdToUtc", createdToUtc?.ToString("O"));
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/carts" : $"api/admin/carts?{string.Join("&", query)}";
    }

    private static string BuildOrdersPath(
        string? status,
        string? paymentStatus,
        string? fulfillmentStatus,
        Guid? customerId,
        Guid? companyId,
        Guid? marketId,
        DateTime? placedFromUtc,
        DateTime? placedToUtc,
        string? search,
        string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "status", status);
        AddQuery(query, "paymentStatus", paymentStatus);
        AddQuery(query, "fulfillmentStatus", fulfillmentStatus);
        AddQuery(query, "customerId", customerId?.ToString());
        AddQuery(query, "companyId", companyId?.ToString());
        AddQuery(query, "marketId", marketId?.ToString());
        AddQuery(query, "placedFromUtc", placedFromUtc?.ToString("O"));
        AddQuery(query, "placedToUtc", placedToUtc?.ToString("O"));
        AddQuery(query, "search", search);
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/orders" : $"api/admin/orders?{string.Join("&", query)}";
    }
}
