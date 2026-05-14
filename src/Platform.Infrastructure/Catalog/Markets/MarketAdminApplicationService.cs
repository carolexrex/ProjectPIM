using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Markets.Commands;
using Platform.Application.Catalog.Markets.Queries;
using Platform.Application.Catalog.Products;
using Platform.Contracts.Catalog.Markets;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Catalog.Markets;

public sealed class MarketAdminApplicationService : IMarketAdminApplicationService
{
    private static readonly string[] AllowedVatModes = ["Gross", "Net"];
    private static readonly string[] AllowedAvailabilityStatuses = ["Active", "Inactive"];

    private readonly IMarketRepository _marketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarketAdminApplicationService(
        IMarketRepository marketRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _marketRepository = marketRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<MarketSummaryDto>> ListAsync(ListMarketsQuery query, CancellationToken cancellationToken)
    {
        var result = await _marketRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        return new PagedResponse<MarketSummaryDto>(result.Items.Select(MapSummary).ToList(), result.Total, page, pageSize);
    }

    public async Task<IReadOnlyList<MarketLookupDto>> ListLookupsAsync(ListMarketLookupsQuery query, CancellationToken cancellationToken)
    {
        var markets = await _marketRepository.ListLookupsAsync(query, cancellationToken);
        return markets.Select(MapLookup).ToList();
    }

    public async Task<MarketDetailsDto?> GetByIdAsync(GetMarketByIdQuery query, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(query.MarketId, cancellationToken);
        return market is null ? null : await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto> CreateAsync(CreateMarketCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeUniqueAsync(command.Code, null, cancellationToken);
        ValidateVatMode(command.VatMode);
        var now = DateTime.UtcNow;
        var market = new Market(Guid.NewGuid(), command.Code, command.Name, command.DefaultCurrency, command.DefaultCulture, command.VatMode, now, now);
        await _marketRepository.AddAsync(market, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto?> UpdateAsync(UpdateMarketCommand command, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(command.Code, command.MarketId, cancellationToken);
        ValidateVatMode(command.VatMode);
        market.Update(command.Code, command.Name, command.DefaultCurrency, command.DefaultCulture, command.VatMode, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto?> ArchiveAsync(ArchiveMarketCommand command, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        market.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto?> AssignCurrenciesAsync(AssignMarketCurrenciesCommand command, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        market.AssignCurrencies(command.DefaultCurrency, command.Currencies, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto?> AssignCulturesAsync(AssignMarketCulturesCommand command, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        market.AssignCultures(command.DefaultCulture, command.Cultures, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto?> UpsertProductAssignmentAsync(UpsertMarketProductAssignmentCommand command, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        ValidateAvailabilityStatus(command.Status);
        if (await _productRepository.GetByIdAsync(command.ProductId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertMarketProductAssignmentCommand.ProductId), "Unknown product.");
        }

        market.UpsertProductAssignment(command.ProductId, command.Status, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    public async Task<MarketDetailsDto?> RemoveProductAssignmentAsync(RemoveMarketProductAssignmentCommand command, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        market.RemoveProductAssignment(command.ProductId, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(market, cancellationToken);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentMarketId, CancellationToken cancellationToken)
    {
        var existing = await _marketRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != currentMarketId)
        {
            throw new ConflictException("Market code already exists.");
        }
    }

    private static void ValidateVatMode(string vatMode)
    {
        if (!AllowedVatModes.Contains(vatMode, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(CreateMarketCommand.VatMode), "Unknown VAT mode.");
        }
    }

    private static void ValidateAvailabilityStatus(string status)
    {
        if (!AllowedAvailabilityStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(UpsertMarketProductAssignmentCommand.Status), "Unknown market availability status.");
        }
    }

    private static MarketSummaryDto MapSummary(Market market)
    {
        return new MarketSummaryDto(market.Id, market.Code, market.Name, market.DefaultCurrency, market.DefaultCulture, market.Status, market.UpdatedAtUtc, market.RowVersion);
    }

    private static MarketLookupDto MapLookup(Market market)
    {
        return new MarketLookupDto(
            market.Id,
            market.Code,
            market.Name,
            market.Currencies
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.CurrencyCode)
                .Select(x => x.CurrencyCode)
                .ToList());
    }

    private async Task<MarketDetailsDto> MapDetailsAsync(Market market, CancellationToken cancellationToken)
    {
        var productIds = market.ProductAssignments.Select(x => x.ProductId).Distinct().ToList();
        var products = productIds.Count == 0 ? [] : await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(x => x.Id);

        return new MarketDetailsDto(
            market.Id,
            market.Code,
            market.Name,
            market.DefaultCurrency,
            market.DefaultCulture,
            market.VatMode,
            market.Status,
            market.Currencies.OrderByDescending(x => x.IsDefault).ThenBy(x => x.CurrencyCode).Select(x => new MarketCurrencyDto(x.CurrencyCode, x.IsDefault)).ToList(),
            market.Cultures.OrderByDescending(x => x.IsDefault).ThenBy(x => x.CultureCode).Select(x => new MarketCultureDto(x.CultureCode, x.IsDefault)).ToList(),
            market.ProductAssignments.Select(x =>
            {
                var product = productMap.GetValueOrDefault(x.ProductId);
                return new MarketProductAssignmentDto(x.ProductId, product?.ProductNumber ?? x.ProductId.ToString(), product?.Translations.FirstOrDefault()?.Name, x.Status);
            }).OrderBy(x => x.ProductNumber).ToList(),
            market.CreatedAtUtc,
            market.UpdatedAtUtc,
            market.RowVersion);
    }
}
