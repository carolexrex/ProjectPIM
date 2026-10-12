using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Tax;
using Platform.Application.Catalog.Tax.Queries;
using Platform.Domain.Catalog.Tax;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Tax;

public sealed class EfVatRuleRepository : IVatRuleRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfVatRuleRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<VatRuleListResult> ListAsync(ListVatRulesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var taxCategoryCode = VatRule.NormalizeTaxCategoryCode(query.TaxCategoryCode);

        var filteredQuery = _dbContext.VatRules
            .AsNoTracking()
            .Where(x => !query.MarketId.HasValue || x.MarketId == query.MarketId.Value)
            .Where(x => taxCategoryCode.Length == 0 || x.TaxCategoryCode == taxCategoryCode);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await filteredQuery
            .OrderBy(x => x.MarketId)
            .ThenBy(x => x.TaxCategoryCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new VatRuleListResult(items, total);
    }

    public async Task<IReadOnlyList<VatRule>> ListByMarketAsync(Guid marketId, CancellationToken cancellationToken)
    {
        return await _dbContext.VatRules
            .AsNoTracking()
            .Where(x => x.MarketId == marketId)
            .OrderBy(x => x.TaxCategoryCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<VatRule?> GetByIdAsync(Guid vatRuleId, CancellationToken cancellationToken)
    {
        return await _dbContext.VatRules.FirstOrDefaultAsync(x => x.Id == vatRuleId, cancellationToken);
    }

    public async Task<VatRule?> GetByMarketAndTaxCategoryAsync(Guid marketId, string taxCategoryCode, CancellationToken cancellationToken)
    {
        var normalized = VatRule.NormalizeTaxCategoryCode(taxCategoryCode);
        return await _dbContext.VatRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MarketId == marketId && x.TaxCategoryCode == normalized, cancellationToken);
    }

    public async Task AddAsync(VatRule vatRule, CancellationToken cancellationToken)
    {
        await _dbContext.VatRules.AddAsync(vatRule, cancellationToken);
    }

    public Task RemoveAsync(VatRule vatRule, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dbContext.VatRules.Remove(vatRule);
        return Task.CompletedTask;
    }
}
