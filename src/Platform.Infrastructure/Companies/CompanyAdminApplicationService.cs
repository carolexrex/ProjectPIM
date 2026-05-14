using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Markets;
using Platform.Application.Companies;
using Platform.Application.Companies.Commands;
using Platform.Application.Companies.Queries;
using Platform.Application.Customers;
using Platform.Contracts.Companies;
using Platform.Contracts.Common;
using Platform.Domain.Companies;
using Platform.Domain.Customers;

namespace Platform.Infrastructure.Companies;

public sealed class CompanyAdminApplicationService : ICompanyAdminApplicationService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyAdminApplicationService(
        ICompanyRepository companyRepository,
        ICustomerRepository customerRepository,
        IMarketRepository marketRepository,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _customerRepository = customerRepository;
        _marketRepository = marketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<CompanySummaryDto>> ListAsync(ListCompaniesQuery query, CancellationToken cancellationToken)
    {
        var result = await _companyRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<CompanySummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<CompanyDetailsDto?> GetByIdAsync(GetCompanyByIdQuery query, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(query.CompanyId, cancellationToken);
        return company is null ? null : await MapDetailsAsync(company, cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyMembershipDto>?> ListMembershipsAsync(ListCompanyMembershipsQuery query, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(query.CompanyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        return await MapMembershipsAsync(company.Memberships, cancellationToken);
    }

    public async Task<CompanyDetailsDto> CreateAsync(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeUniqueAsync(command.Code, null, cancellationToken);
        await EnsureMarketExistsAsync(command.DefaultMarketId, cancellationToken);

        var now = DateTime.UtcNow;
        var company = new Company(
            Guid.NewGuid(),
            command.ExternalId,
            command.Code,
            command.Name,
            command.LegalName,
            command.OrganizationNumber,
            command.VatNumber,
            command.Email,
            command.Phone,
            command.DefaultMarketId,
            command.DefaultCurrency,
            command.Status,
            now,
            now);

        await _companyRepository.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(company, cancellationToken);
    }

    public async Task<CompanyDetailsDto?> UpdateAsync(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(command.Code, command.CompanyId, cancellationToken);
        await EnsureMarketExistsAsync(command.DefaultMarketId, cancellationToken);

        company.Update(
            command.ExternalId,
            command.Code,
            command.Name,
            command.LegalName,
            command.OrganizationNumber,
            command.VatNumber,
            command.Email,
            command.Phone,
            command.DefaultMarketId,
            command.DefaultCurrency,
            command.Status,
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(company, cancellationToken);
    }

    public async Task<CompanyAddressDto?> AddAddressAsync(AddCompanyAddressCommand command, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var address = company.AddAddress(
            command.Type,
            command.Attention,
            command.Line1,
            command.Line2,
            command.PostalCode,
            command.City,
            command.Region,
            command.CountryCode,
            command.Email,
            command.Phone,
            command.IsDefault);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapAddress(address);
    }

    public async Task<CompanyMembershipDto?> CreateMembershipAsync(CreateCompanyMembershipCommand command, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new RequestValidationException(nameof(CreateCompanyMembershipCommand.CustomerId), "Unknown customer.");
        }

        ValidateMembershipWindow(command.ValidFromUtc, command.ValidToUtc);

        if (company.Memberships.Any(x => x.CustomerId == command.CustomerId))
        {
            throw new ConflictException("Customer is already a member of this company.");
        }

        var membership = company.AddMembership(
            command.CustomerId,
            command.Role,
            command.Status,
            command.IsDefaultCompany,
            command.CanPlaceOrders,
            command.CanApproveOrders,
            command.CanManageUsers,
            command.ValidFromUtc,
            command.ValidToUtc);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapMembership(membership, customer);
    }

    public async Task<CompanyMembershipDto?> UpdateMembershipAsync(UpdateCompanyMembershipCommand command, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByMembershipIdAsync(command.MembershipId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var membership = company.GetMembership(command.MembershipId);
        if (membership is null)
        {
            return null;
        }

        ValidateMembershipWindow(command.ValidFromUtc, command.ValidToUtc);

        membership.Update(
            command.Role,
            command.Status,
            command.IsDefaultCompany,
            command.CanPlaceOrders,
            command.CanApproveOrders,
            command.CanManageUsers,
            command.ValidFromUtc,
            command.ValidToUtc,
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(membership.CustomerId, cancellationToken)
            ?? throw new RequestValidationException(nameof(UpdateCompanyMembershipCommand.MembershipId), "Unknown customer for membership.");

        return MapMembership(membership, customer);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentCompanyId, CancellationToken cancellationToken)
    {
        var existing = await _companyRepository.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null && existing.Id != currentCompanyId)
        {
            throw new ConflictException("Company code already exists.");
        }
    }

    private async Task EnsureMarketExistsAsync(Guid? marketId, CancellationToken cancellationToken)
    {
        if (!marketId.HasValue)
        {
            return;
        }

        if (await _marketRepository.GetByIdAsync(marketId.Value, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(CreateCompanyCommand.DefaultMarketId), "Unknown market.");
        }
    }

    private static void ValidateMembershipWindow(DateTime? validFromUtc, DateTime? validToUtc)
    {
        if (validFromUtc.HasValue && validToUtc.HasValue && validToUtc.Value < validFromUtc.Value)
        {
            throw new RequestValidationException(nameof(CreateCompanyMembershipCommand.ValidToUtc), "Valid-to date must be greater than or equal to valid-from date.");
        }
    }

    private async Task<CompanyDetailsDto> MapDetailsAsync(Company company, CancellationToken cancellationToken)
    {
        var memberships = await MapMembershipsAsync(company.Memberships, cancellationToken);

        return new CompanyDetailsDto(
            company.Id,
            company.ExternalId,
            company.Code,
            company.Name,
            company.LegalName,
            company.OrganizationNumber,
            company.VatNumber,
            company.Email,
            company.Phone,
            company.DefaultMarketId,
            company.DefaultCurrency,
            company.Status,
            company.Addresses.Select(MapAddress).ToList(),
            memberships,
            company.CreatedAtUtc,
            company.UpdatedAtUtc,
            company.RowVersion);
    }

    private async Task<IReadOnlyList<CompanyMembershipDto>> MapMembershipsAsync(IEnumerable<CompanyMembership> memberships, CancellationToken cancellationToken)
    {
        var list = memberships.ToList();
        var customerMap = (await _customerRepository.GetByIdsAsync(list.Select(x => x.CustomerId).Distinct().ToList(), cancellationToken))
            .ToDictionary(x => x.Id);

        return list
            .Select(membership =>
            {
                customerMap.TryGetValue(membership.CustomerId, out var customer);
                return MapMembership(membership, customer);
            })
            .OrderBy(x => x.CustomerDisplayName)
            .ToList();
    }

    private static CompanySummaryDto MapSummary(Company company)
    {
        return new CompanySummaryDto(
            company.Id,
            company.Code,
            company.Name,
            company.Status,
            company.DefaultMarketId,
            company.DefaultCurrency,
            company.Memberships.Count,
            company.UpdatedAtUtc,
            company.RowVersion);
    }

    private static CompanyAddressDto MapAddress(CompanyAddress address)
    {
        return new CompanyAddressDto(
            address.Id,
            address.CompanyId,
            address.Type,
            address.Attention,
            address.Line1,
            address.Line2,
            address.PostalCode,
            address.City,
            address.Region,
            address.CountryCode,
            address.Email,
            address.Phone,
            address.IsDefault);
    }

    private static CompanyMembershipDto MapMembership(CompanyMembership membership, Customer? customer)
    {
        var displayName = customer is null
            ? membership.CustomerId.ToString()
            : $"{customer.FirstName} {customer.LastName}".Trim();

        return new CompanyMembershipDto(
            membership.Id,
            membership.CompanyId,
            membership.CustomerId,
            customer?.Email ?? string.Empty,
            string.IsNullOrWhiteSpace(displayName) ? customer?.Email ?? membership.CustomerId.ToString() : displayName,
            membership.Role,
            membership.Status,
            membership.IsDefaultCompany,
            membership.CanPlaceOrders,
            membership.CanApproveOrders,
            membership.CanManageUsers,
            membership.ValidFromUtc,
            membership.ValidToUtc,
            membership.IsValidAt(DateTime.UtcNow),
            membership.RowVersion);
    }
}
