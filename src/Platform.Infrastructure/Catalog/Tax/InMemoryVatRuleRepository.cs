using Platform.Application.Abstractions.Errors;
using Platform.Application.Catalog.Tax;
using Platform.Application.Catalog.Tax.Queries;
using Platform.Domain.Catalog.Tax;

namespace Platform.Infrastructure.Catalog.Tax;

public sealed class InMemoryVatRuleRepository : IVatRuleRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryVatRuleRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<VatRuleListResult> ListAsync(ListVatRulesQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var taxCategoryCode = VatRule.NormalizeTaxCategoryCode(query.TaxCategoryCode);

        var filtered = _store.VatRules.Values
            .Where(x => !query.MarketId.HasValue || x.MarketId == query.MarketId.Value)
            .Where(x => taxCategoryCode.Length == 0 || x.TaxCategoryCode == taxCategoryCode)
            .OrderBy(x => x.MarketId)
            .ThenBy(x => x.TaxCategoryCode, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(new VatRuleListResult(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), filtered.Count));
    }

    public Task<IReadOnlyList<VatRule>> ListByMarketAsync(Guid marketId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<VatRule> items = _store.VatRules.Values
            .Where(x => x.MarketId == marketId)
            .OrderBy(x => x.TaxCategoryCode, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<VatRule?> GetByIdAsync(Guid vatRuleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.VatRules.TryGetValue(vatRuleId, out var vatRule) ? vatRule : null);
    }

    public Task<VatRule?> GetByMarketAndTaxCategoryAsync(Guid marketId, string taxCategoryCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = VatRule.NormalizeTaxCategoryCode(taxCategoryCode);
        return Task.FromResult(_store.VatRules.Values.FirstOrDefault(x => x.MarketId == marketId && x.TaxCategoryCode == normalized));
    }

    public Task AddAsync(VatRule vatRule, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Mirrors the PostgreSQL unique index on (market, tax category) for concurrent in-memory writers.
        lock (_store.VatRules)
        {
            if (_store.VatRules.Values.Any(x => x.MarketId == vatRule.MarketId && x.TaxCategoryCode == vatRule.TaxCategoryCode))
            {
                throw new ConflictException("A VAT rule already exists for this market and tax category.");
            }

            _store.VatRules[vatRule.Id] = vatRule;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(VatRule vatRule, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.VatRules.TryRemove(vatRule.Id, out _);
        return Task.CompletedTask;
    }
}
