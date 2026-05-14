using System.Net;
using System.Net.Http.Json;
using Platform.Contracts.Companies;
using Platform.Contracts.Common;
using Platform.Contracts.Customers;

namespace Platform.Backoffice.Integration;

public sealed partial class AdminApiClient
{
    public async Task<PagedResponse<CustomerSummaryDto>> ListCustomersAsync(
        string? search,
        string? status,
        bool? isGuest,
        Guid? defaultMarketId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildCustomersPath(search, status, isGuest, defaultMarketId, sort);
        return await GetRequiredAsync<PagedResponse<CustomerSummaryDto>>(path, cancellationToken);
    }

    public async Task<CustomerDetailsDto?> GetCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/customers/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CustomerDetailsDto>(response, cancellationToken);
    }

    public async Task<CustomerDetailsDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/customers", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CustomerDetailsDto>(response, cancellationToken);
    }

    public async Task<CustomerDetailsDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/customers/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CustomerDetailsDto>(response, cancellationToken);
    }

    public async Task<CustomerAddressDto?> AddCustomerAddressAsync(Guid id, AddCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/customers/{id}/addresses", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CustomerAddressDto>(response, cancellationToken);
    }

    public async Task<PagedResponse<CompanySummaryDto>> ListCompaniesAsync(
        string? search,
        string? status,
        Guid? defaultMarketId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var path = BuildCompaniesPath(search, status, defaultMarketId, sort);
        return await GetRequiredAsync<PagedResponse<CompanySummaryDto>>(path, cancellationToken);
    }

    public async Task<CompanyDetailsDto?> GetCompanyAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/companies/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CompanyDetailsDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyMembershipDto>?> ListCompanyMembershipsAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/admin/companies/{id}/memberships", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<IReadOnlyList<CompanyMembershipDto>>(response, cancellationToken);
    }

    public async Task<CompanyDetailsDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/admin/companies", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CompanyDetailsDto>(response, cancellationToken);
    }

    public async Task<CompanyDetailsDto?> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/companies/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CompanyDetailsDto>(response, cancellationToken);
    }

    public async Task<CompanyAddressDto?> AddCompanyAddressAsync(Guid id, AddCompanyAddressRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/companies/{id}/addresses", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CompanyAddressDto>(response, cancellationToken);
    }

    public async Task<CompanyMembershipDto?> CreateCompanyMembershipAsync(Guid id, CreateCompanyMembershipRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/admin/companies/{id}/memberships", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CompanyMembershipDto>(response, cancellationToken);
    }

    public async Task<CompanyMembershipDto?> UpdateCompanyMembershipAsync(Guid id, UpdateCompanyMembershipRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/admin/company-memberships/{id}", request, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<CompanyMembershipDto>(response, cancellationToken);
    }

    private static string BuildCustomersPath(string? search, string? status, bool? isGuest, Guid? defaultMarketId, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "isGuest", isGuest?.ToString()?.ToLowerInvariant());
        AddQuery(query, "defaultMarketId", defaultMarketId?.ToString());
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/customers" : $"api/admin/customers?{string.Join("&", query)}";
    }

    private static string BuildCompaniesPath(string? search, string? status, Guid? defaultMarketId, string? sort)
    {
        var query = new List<string>();
        AddQuery(query, "search", search);
        AddQuery(query, "status", status);
        AddQuery(query, "defaultMarketId", defaultMarketId?.ToString());
        AddQuery(query, "sort", sort);
        return query.Count == 0 ? "api/admin/companies" : $"api/admin/companies?{string.Join("&", query)}";
    }
}
