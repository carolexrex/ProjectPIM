using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Customers;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("customers")]
public sealed class CustomersController : Controller
{
    private static readonly IReadOnlyList<string> StatusOptions = ["Active", "Inactive"];
    private static readonly IReadOnlyList<string> AddressTypeOptions = ["Shipping", "Billing"];

    private readonly IAdminApiClient _apiClient;

    public CustomersController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, bool? isGuest, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListCustomersAsync(search, status, isGuest, null, "email", cancellationToken);
        return View(new CustomerListPageViewModel
        {
            Search = search,
            Status = status,
            IsGuest = isGuest,
            Customers = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        return View(await BuildCreateViewModelAsync(cancellationToken));
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(CustomerCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCustomerFormOptionsAsync(form, cancellationToken);
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateCustomerAsync(
                new CreateCustomerRequest
                {
                    ExternalId = form.ExternalId,
                    UserId = form.UserId,
                    Email = form.Email,
                    FirstName = form.FirstName,
                    LastName = form.LastName,
                    Phone = form.Phone,
                    PreferredCulture = form.PreferredCulture,
                    DefaultMarketId = form.DefaultMarketId,
                    Status = form.Status,
                    IsGuest = form.IsGuest
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Customer {created.Email} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            await PopulateCustomerFormOptionsAsync(form, cancellationToken);
            return View(form);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Customer")] CustomerUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, customerForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateCustomerAsync(
                id,
                new UpdateCustomerRequest
                {
                    ExternalId = form.ExternalId,
                    UserId = form.UserId,
                    Email = form.Email,
                    FirstName = form.FirstName,
                    LastName = form.LastName,
                    Phone = form.Phone,
                    PreferredCulture = form.PreferredCulture,
                    DefaultMarketId = form.DefaultMarketId,
                    Status = form.Status,
                    IsGuest = form.IsGuest,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Customer {updated.Email} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Customer");
            var invalidPage = await BuildDetailsPageAsync(id, customerForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/addresses")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(Guid id, [Bind(Prefix = "AddressForm")] CustomerAddressCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, addressForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var address = await _apiClient.AddCustomerAddressAsync(
                id,
                new AddCustomerAddressRequest
                {
                    Type = form.Type,
                    Attention = form.Attention,
                    FirstName = form.FirstName,
                    LastName = form.LastName,
                    CompanyName = form.CompanyName,
                    Line1 = form.Line1,
                    Line2 = form.Line2,
                    PostalCode = form.PostalCode,
                    City = form.City,
                    Region = form.Region,
                    CountryCode = form.CountryCode,
                    Phone = form.Phone,
                    Email = form.Email,
                    IsDefault = form.IsDefault
                },
                cancellationToken);

            if (address is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Customer address added.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "AddressForm");
            var invalidPage = await BuildDetailsPageAsync(id, addressForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<CustomerCreateViewModel> BuildCreateViewModelAsync(CancellationToken cancellationToken)
    {
        var form = new CustomerCreateViewModel { Status = StatusOptions[0] };
        await PopulateCustomerFormOptionsAsync(form, cancellationToken);
        return form;
    }

    private async Task<CustomerDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid customerId,
        CustomerUpdateViewModel? customerForm = null,
        CustomerAddressCreateViewModel? addressForm = null,
        CancellationToken cancellationToken = default)
    {
        var customer = await _apiClient.GetCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        customerForm ??= new CustomerUpdateViewModel
        {
            Id = customer.Id,
            ExternalId = customer.ExternalId,
            UserId = customer.UserId,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone,
            PreferredCulture = customer.PreferredCulture,
            DefaultMarketId = customer.DefaultMarketId,
            Status = customer.Status,
            IsGuest = customer.IsGuest,
            RowVersion = customer.RowVersion,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
        await PopulateCustomerFormOptionsAsync(customerForm, cancellationToken);

        addressForm ??= new CustomerAddressCreateViewModel
        {
            CustomerId = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Type = AddressTypeOptions[0],
            CountryCode = "SE"
        };
        addressForm.TypeOptions = AddressTypeOptions;

        return new CustomerDetailsPageViewModel
        {
            Customer = customerForm,
            Addresses = customer.Addresses,
            AddressForm = addressForm
        };
    }

    private async Task PopulateCustomerFormOptionsAsync(CustomerCreateViewModel form, CancellationToken cancellationToken)
    {
        form.StatusOptions = StatusOptions;
        var markets = await _apiClient.ListMarketLookupsAsync(null, "Active", null, cancellationToken);
        form.MarketOptions = markets
            .Select(x => new MarketLookupOptionViewModel(x.Id, x.Code, $"{x.Name} ({x.Code})"))
            .OrderBy(x => x.Label)
            .ToList();
    }

    private void ApplyApiErrors(AdminApiException exception, string? prefix = null)
    {
        if (exception.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var error in exception.Errors)
        {
            var key = string.IsNullOrWhiteSpace(prefix) ? error.Key : $"{prefix}.{error.Key}";
            foreach (var message in error.Value)
            {
                ModelState.AddModelError(key, message);
            }
        }
    }
}
