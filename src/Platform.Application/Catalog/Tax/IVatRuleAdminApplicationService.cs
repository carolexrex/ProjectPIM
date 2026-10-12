using Platform.Application.Catalog.Tax.Commands;
using Platform.Application.Catalog.Tax.Queries;
using Platform.Contracts.Catalog.Tax;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Tax;

public interface IVatRuleAdminApplicationService
{
    Task<PagedResponse<VatRuleDto>> ListAsync(ListVatRulesQuery query, CancellationToken cancellationToken);
    Task<VatRuleDto?> GetByIdAsync(GetVatRuleByIdQuery query, CancellationToken cancellationToken);
    Task<VatRuleDto> CreateAsync(CreateVatRuleCommand command, CancellationToken cancellationToken);
    Task<VatRuleDto?> UpdateAsync(UpdateVatRuleCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(DeleteVatRuleCommand command, CancellationToken cancellationToken);
}
