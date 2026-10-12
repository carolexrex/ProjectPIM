using Platform.Application.Catalog.Tax.Queries;
using Platform.Domain.Catalog.Tax;

namespace Platform.Application.Catalog.Tax;

public interface IVatRuleRepository
{
    Task<VatRuleListResult> ListAsync(ListVatRulesQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<VatRule>> ListByMarketAsync(Guid marketId, CancellationToken cancellationToken);
    Task<VatRule?> GetByIdAsync(Guid vatRuleId, CancellationToken cancellationToken);
    Task<VatRule?> GetByMarketAndTaxCategoryAsync(Guid marketId, string taxCategoryCode, CancellationToken cancellationToken);
    Task AddAsync(VatRule vatRule, CancellationToken cancellationToken);
    Task RemoveAsync(VatRule vatRule, CancellationToken cancellationToken);
}
