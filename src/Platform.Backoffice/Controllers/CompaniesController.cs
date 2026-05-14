using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Companies;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("companies")]
public sealed class CompaniesController : Controller
{
    private static readonly IReadOnlyList<string> StatusOptions = ["Active", "Inactive"];
    private static readonly IReadOnlyList<string> AddressTypeOptions = ["Billing", "Shipping"];
    private static readonly IReadOnlyList<string> MembershipRoleOptions = ["Buyer", "Admin", "Approver", "Contact"];

    private readonly IAdminApiClient _apiClient;

    public CompaniesController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListCompaniesAsync(search, status, null, "code", cancellationToken);
        return View(new CompanyListPageViewModel
        {
            Search = search,
            Status = status,
            Companies = response.Items,
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
    public async Task<IActionResult> New(CompanyCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCompanyFormOptionsAsync(form, cancellationToken);
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateCompanyAsync(
                new CreateCompanyRequest
                {
                    ExternalId = form.ExternalId,
                    Code = form.Code,
                    Name = form.Name,
                    LegalName = form.LegalName,
                    OrganizationNumber = form.OrganizationNumber,
                    VatNumber = form.VatNumber,
                    Email = form.Email,
                    Phone = form.Phone,
                    DefaultMarketId = form.DefaultMarketId,
                    DefaultCurrency = form.DefaultCurrency,
                    Status = form.Status
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Company {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            await PopulateCompanyFormOptionsAsync(form, cancellationToken);
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
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Company")] CompanyUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, companyForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateCompanyAsync(
                id,
                new UpdateCompanyRequest
                {
                    ExternalId = form.ExternalId,
                    Code = form.Code,
                    Name = form.Name,
                    LegalName = form.LegalName,
                    OrganizationNumber = form.OrganizationNumber,
                    VatNumber = form.VatNumber,
                    Email = form.Email,
                    Phone = form.Phone,
                    DefaultMarketId = form.DefaultMarketId,
                    DefaultCurrency = form.DefaultCurrency,
                    Status = form.Status,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Company {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Company");
            var invalidPage = await BuildDetailsPageAsync(id, companyForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/addresses")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(Guid id, [Bind(Prefix = "AddressForm")] CompanyAddressCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, addressForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var address = await _apiClient.AddCompanyAddressAsync(
                id,
                new AddCompanyAddressRequest
                {
                    Type = form.Type,
                    Attention = form.Attention,
                    Line1 = form.Line1,
                    Line2 = form.Line2,
                    PostalCode = form.PostalCode,
                    City = form.City,
                    Region = form.Region,
                    CountryCode = form.CountryCode,
                    Email = form.Email,
                    Phone = form.Phone,
                    IsDefault = form.IsDefault
                },
                cancellationToken);

            if (address is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Company address added.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "AddressForm");
            var invalidPage = await BuildDetailsPageAsync(id, addressForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/memberships")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMembership(Guid id, [Bind(Prefix = "MembershipForm")] CompanyMembershipCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, membershipForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var membership = await _apiClient.CreateCompanyMembershipAsync(
                id,
                new CreateCompanyMembershipRequest
                {
                    CustomerId = form.CustomerId!.Value,
                    Role = form.Role,
                    Status = form.Status,
                    IsDefaultCompany = form.IsDefaultCompany,
                    CanPlaceOrders = form.CanPlaceOrders,
                    CanApproveOrders = form.CanApproveOrders,
                    CanManageUsers = form.CanManageUsers,
                    ValidFromUtc = form.ValidFromUtc,
                    ValidToUtc = form.ValidToUtc
                },
                cancellationToken);

            if (membership is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Company membership added.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "MembershipForm");
            var invalidPage = await BuildDetailsPageAsync(id, membershipForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/memberships/{membershipId:guid}")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMembership(
        Guid id,
        Guid membershipId,
        [Bind(Prefix = "Membership")] CompanyMembershipUpdateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, membershipId: membershipId, membershipUpdateForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateCompanyMembershipAsync(
                membershipId,
                new UpdateCompanyMembershipRequest
                {
                    Role = form.Role,
                    Status = form.Status,
                    IsDefaultCompany = form.IsDefaultCompany,
                    CanPlaceOrders = form.CanPlaceOrders,
                    CanApproveOrders = form.CanApproveOrders,
                    CanManageUsers = form.CanManageUsers,
                    ValidFromUtc = form.ValidFromUtc,
                    ValidToUtc = form.ValidToUtc,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Membership for {form.CustomerDisplayName} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Membership");
            var invalidPage = await BuildDetailsPageAsync(id, membershipId: membershipId, membershipUpdateForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<CompanyCreateViewModel> BuildCreateViewModelAsync(CancellationToken cancellationToken)
    {
        var form = new CompanyCreateViewModel { Status = StatusOptions[0] };
        await PopulateCompanyFormOptionsAsync(form, cancellationToken);
        return form;
    }

    private async Task<CompanyDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid companyId,
        CompanyUpdateViewModel? companyForm = null,
        CompanyAddressCreateViewModel? addressForm = null,
        CompanyMembershipCreateViewModel? membershipForm = null,
        Guid? membershipId = null,
        CompanyMembershipUpdateViewModel? membershipUpdateForm = null,
        CancellationToken cancellationToken = default)
    {
        var companyTask = _apiClient.GetCompanyAsync(companyId, cancellationToken);
        var customersTask = _apiClient.ListCustomersAsync(null, "Active", false, null, "email", cancellationToken);
        await Task.WhenAll(companyTask, customersTask);

        var company = await companyTask;
        if (company is null)
        {
            return null;
        }

        companyForm ??= new CompanyUpdateViewModel
        {
            Id = company.Id,
            ExternalId = company.ExternalId,
            Code = company.Code,
            Name = company.Name,
            LegalName = company.LegalName,
            OrganizationNumber = company.OrganizationNumber,
            VatNumber = company.VatNumber,
            Email = company.Email,
            Phone = company.Phone,
            DefaultMarketId = company.DefaultMarketId,
            DefaultCurrency = company.DefaultCurrency,
            Status = company.Status,
            RowVersion = company.RowVersion,
            CreatedAtUtc = company.CreatedAtUtc,
            UpdatedAtUtc = company.UpdatedAtUtc
        };
        await PopulateCompanyFormOptionsAsync(companyForm, cancellationToken);

        addressForm ??= new CompanyAddressCreateViewModel
        {
            CompanyId = company.Id,
            Type = AddressTypeOptions[0],
            CountryCode = "SE"
        };
        addressForm.TypeOptions = AddressTypeOptions;

        var customerOptions = (await customersTask).Items
            .Where(x => company.Memberships.All(m => m.CustomerId != x.Id))
            .Select(x => new CustomerLookupOptionViewModel(x.Id, x.Email, $"{x.FirstName} {x.LastName} ({x.Email})".Trim()))
            .OrderBy(x => x.Label)
            .ToList();

        membershipForm ??= new CompanyMembershipCreateViewModel
        {
            CompanyId = company.Id,
            Role = MembershipRoleOptions[0],
            Status = StatusOptions[0],
            ValidFromUtc = DateTime.UtcNow.Date
        };
        membershipForm.RoleOptions = MembershipRoleOptions;
        membershipForm.StatusOptions = StatusOptions;
        membershipForm.CustomerOptions = customerOptions;

        var membershipForms = company.Memberships
            .Select(x =>
            {
                if (membershipUpdateForm is not null && membershipId == x.Id)
                {
                    membershipUpdateForm.RoleOptions = MembershipRoleOptions;
                    membershipUpdateForm.StatusOptions = StatusOptions;
                    return membershipUpdateForm;
                }

                return new CompanyMembershipUpdateViewModel
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    CustomerEmail = x.CustomerEmail,
                    CustomerDisplayName = x.CustomerDisplayName,
                    Role = x.Role,
                    Status = x.Status,
                    IsDefaultCompany = x.IsDefaultCompany,
                    CanPlaceOrders = x.CanPlaceOrders,
                    CanApproveOrders = x.CanApproveOrders,
                    CanManageUsers = x.CanManageUsers,
                    ValidFromUtc = x.ValidFromUtc,
                    ValidToUtc = x.ValidToUtc,
                    RowVersion = x.RowVersion,
                    RoleOptions = MembershipRoleOptions,
                    StatusOptions = StatusOptions
                };
            })
            .ToList();

        return new CompanyDetailsPageViewModel
        {
            Company = companyForm,
            Addresses = company.Addresses,
            Memberships = membershipForms,
            AddressForm = addressForm,
            MembershipForm = membershipForm
        };
    }

    private async Task PopulateCompanyFormOptionsAsync(CompanyCreateViewModel form, CancellationToken cancellationToken)
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
