using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Tax;
using Platform.Application.Catalog.Tax.Commands;
using Platform.Application.Catalog.Tax.Queries;
using Platform.Contracts.Catalog.Tax;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Tax;

namespace Platform.Infrastructure.Catalog.Tax;

public sealed class VatRuleAdminApplicationService : IVatRuleAdminApplicationService
{
    private readonly IVatRuleRepository _vatRuleRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VatRuleAdminApplicationService(
        IVatRuleRepository vatRuleRepository,
        IMarketRepository marketRepository,
        IUnitOfWork unitOfWork)
    {
        _vatRuleRepository = vatRuleRepository;
        _marketRepository = marketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<VatRuleDto>> ListAsync(ListVatRulesQuery query, CancellationToken cancellationToken)
    {
        var result = await _vatRuleRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var markets = await LoadMarketsAsync(result.Items, cancellationToken);

        return new PagedResponse<VatRuleDto>(
            result.Items.Select(x => Map(x, markets)).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<VatRuleDto?> GetByIdAsync(GetVatRuleByIdQuery query, CancellationToken cancellationToken)
    {
        var vatRule = await _vatRuleRepository.GetByIdAsync(query.VatRuleId, cancellationToken);
        return vatRule is null ? null : await MapAsync(vatRule, cancellationToken);
    }

    public async Task<VatRuleDto> CreateAsync(CreateVatRuleCommand command, CancellationToken cancellationToken)
    {
        var taxCategoryCode = VatRule.NormalizeTaxCategoryCode(command.TaxCategoryCode);
        if (taxCategoryCode.Length == 0 || taxCategoryCode.Length > VatRule.MaxTaxCategoryCodeLength)
        {
            throw new RequestValidationException(
                nameof(CreateVatRuleCommand.TaxCategoryCode),
                $"Tax category code is required and must not exceed {VatRule.MaxTaxCategoryCodeLength} characters.");
        }

        ValidateRate(command.Rate);
        ValidateDescription(command.Description);

        if (await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(CreateVatRuleCommand.MarketId), "Unknown market.");
        }

        if (await _vatRuleRepository.GetByMarketAndTaxCategoryAsync(command.MarketId, taxCategoryCode, cancellationToken) is not null)
        {
            throw new ConflictException("A VAT rule already exists for this market and tax category.");
        }

        var now = DateTime.UtcNow;
        var vatRule = new VatRule(Guid.NewGuid(), command.MarketId, taxCategoryCode, command.Rate, command.Description, now, now);
        await _vatRuleRepository.AddAsync(vatRule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapAsync(vatRule, cancellationToken);
    }

    public async Task<VatRuleDto?> UpdateAsync(UpdateVatRuleCommand command, CancellationToken cancellationToken)
    {
        var vatRule = await _vatRuleRepository.GetByIdAsync(command.VatRuleId, cancellationToken);
        if (vatRule is null)
        {
            return null;
        }

        ValidateRate(command.Rate);
        ValidateDescription(command.Description);
        vatRule.Update(command.Rate, command.Description, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapAsync(vatRule, cancellationToken);
    }

    public async Task<bool> DeleteAsync(DeleteVatRuleCommand command, CancellationToken cancellationToken)
    {
        var vatRule = await _vatRuleRepository.GetByIdAsync(command.VatRuleId, cancellationToken);
        if (vatRule is null)
        {
            return false;
        }

        vatRule.EnsureRowVersion(command.RowVersion);
        await _vatRuleRepository.RemoveAsync(vatRule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateRate(decimal rate)
    {
        if (!VatRule.IsValidRate(rate))
        {
            throw new RequestValidationException(
                nameof(CreateVatRuleCommand.Rate),
                $"Rate must be a fraction from 0 up to (but excluding) 1 with at most {VatRule.RateDecimalPlaces} decimal places, e.g. 0.25 for 25%.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Trim().Length > VatRule.MaxDescriptionLength)
        {
            throw new RequestValidationException(
                nameof(CreateVatRuleCommand.Description),
                $"Description must not exceed {VatRule.MaxDescriptionLength} characters.");
        }
    }

    private async Task<VatRuleDto> MapAsync(VatRule vatRule, CancellationToken cancellationToken)
    {
        var markets = await LoadMarketsAsync([vatRule], cancellationToken);
        return Map(vatRule, markets);
    }

    private async Task<IReadOnlyDictionary<Guid, Market>> LoadMarketsAsync(IReadOnlyCollection<VatRule> vatRules, CancellationToken cancellationToken)
    {
        var marketIds = vatRules.Select(x => x.MarketId).Distinct().ToList();
        if (marketIds.Count == 0)
        {
            return new Dictionary<Guid, Market>();
        }

        var markets = await _marketRepository.GetByIdsAsync(marketIds, cancellationToken);
        return markets.ToDictionary(x => x.Id);
    }

    private static VatRuleDto Map(VatRule vatRule, IReadOnlyDictionary<Guid, Market> markets)
    {
        return new VatRuleDto(
            vatRule.Id,
            vatRule.MarketId,
            markets.GetValueOrDefault(vatRule.MarketId)?.Code ?? vatRule.MarketId.ToString(),
            vatRule.TaxCategoryCode,
            vatRule.Rate,
            vatRule.Description,
            vatRule.CreatedAtUtc,
            vatRule.UpdatedAtUtc,
            vatRule.RowVersion);
    }
}
