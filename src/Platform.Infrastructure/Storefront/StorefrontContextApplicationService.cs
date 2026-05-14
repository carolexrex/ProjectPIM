using Platform.Application.Catalog.Channels;
using Platform.Application.Catalog.Markets;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Channels;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontContextApplicationService : IStorefrontContextApplicationService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IMarketRepository _marketRepository;

    public StorefrontContextApplicationService(
        IChannelRepository channelRepository,
        IMarketRepository marketRepository)
    {
        _channelRepository = channelRepository;
        _marketRepository = marketRepository;
    }

    public async Task<StorefrontContextResolutionResult> GetContextAsync(GetStorefrontContextQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedChannelCode = NormalizeOptional(query.ChannelCode);
        var normalizedMarketCode = NormalizeOptional(query.MarketCode);
        var normalizedCultureCode = NormalizeOptional(query.CultureCode);
        var normalizedCurrencyCode = NormalizeOptional(query.CurrencyCode)?.ToUpperInvariant();
        var normalizedHostName = NormalizeOptional(query.HostName);

        if (normalizedChannelCode is null && normalizedMarketCode is null && normalizedHostName is null)
        {
            return StorefrontContextResolutionResult.Invalid(
                nameof(query.MarketCode),
                "Either market, channel, or a resolvable host name is required.");
        }

        Channel? channel = null;
        if (normalizedChannelCode is not null)
        {
            channel = await _channelRepository.GetByCodeAsync(normalizedChannelCode, cancellationToken);
            if (channel is null || !IsActive(channel.Status))
            {
                return StorefrontContextResolutionResult.NotFound("Channel", normalizedChannelCode);
            }
        }
        else if (normalizedHostName is not null)
        {
            channel = await _channelRepository.GetByHostNameAsync(normalizedHostName, cancellationToken);
            if (channel is not null && !IsActive(channel.Status))
            {
                return StorefrontContextResolutionResult.NotFound("Channel", normalizedHostName);
            }
        }

        var marketResolution = await ResolveMarketAsync(channel, normalizedMarketCode, cancellationToken);
        if (marketResolution.Result is not null)
        {
            return marketResolution.Result;
        }

        var market = marketResolution.Market!;
        var activeCultureCode = ResolveCultureCode(market, normalizedCultureCode);
        var activeCurrencyCode = ResolveCurrencyCode(market, normalizedCurrencyCode);

        var context = new StorefrontContextDto(
            channel is null
                ? null
                : new StorefrontChannelContextDto(channel.Id, channel.Code, channel.Name, channel.HostName),
            new StorefrontMarketContextDto(
                market.Id,
                market.Code,
                market.Name,
                market.DefaultCurrency,
                market.DefaultCulture,
                market.VatMode),
            activeCultureCode,
            activeCurrencyCode,
            market.Cultures
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.CultureCode, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.CultureCode)
                .ToList(),
            market.Currencies
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.CurrencyCode, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.CurrencyCode)
                .ToList());

        return StorefrontContextResolutionResult.Success(context);
    }

    private async Task<(Market? Market, StorefrontContextResolutionResult? Result)> ResolveMarketAsync(
        Channel? channel,
        string? marketCode,
        CancellationToken cancellationToken)
    {
        if (marketCode is not null)
        {
            var marketByCode = await _marketRepository.GetByCodeAsync(marketCode, cancellationToken);
            if (marketByCode is null || !IsActive(marketByCode.Status))
            {
                return (null, StorefrontContextResolutionResult.NotFound("Market", marketCode));
            }

            if (channel is not null && channel.MarketAssignments.All(x => x.MarketId != marketByCode.Id))
            {
                return (null, StorefrontContextResolutionResult.NotFound("ChannelMarketAssignment", $"{channel.Code}:{marketCode}"));
            }

            return (marketByCode, null);
        }

        if (channel is null)
        {
            return (null, StorefrontContextResolutionResult.Invalid(
                nameof(GetStorefrontContextQuery.MarketCode),
                "Market is required when no channel or host name resolves it."));
        }

        var assignedMarketIds = channel.MarketAssignments
            .Select(x => x.MarketId)
            .Distinct()
            .ToList();

        if (assignedMarketIds.Count == 0)
        {
            return (null, StorefrontContextResolutionResult.NotFound("ChannelMarketAssignment", channel.Code));
        }

        if (assignedMarketIds.Count > 1)
        {
            return (null, StorefrontContextResolutionResult.Invalid(
                nameof(GetStorefrontContextQuery.MarketCode),
                "Market is required when the channel maps to multiple markets."));
        }

        var market = await _marketRepository.GetByIdAsync(assignedMarketIds[0], cancellationToken);
        if (market is null || !IsActive(market.Status))
        {
            return (null, StorefrontContextResolutionResult.NotFound("Market", assignedMarketIds[0].ToString()));
        }

        return (market, null);
    }

    private static string ResolveCultureCode(Market market, string? requestedCultureCode)
    {
        if (requestedCultureCode is not null
            && market.Cultures.Any(x => string.Equals(x.CultureCode, requestedCultureCode, StringComparison.OrdinalIgnoreCase)))
        {
            return market.Cultures
                .First(x => string.Equals(x.CultureCode, requestedCultureCode, StringComparison.OrdinalIgnoreCase))
                .CultureCode;
        }

        return market.DefaultCulture;
    }

    private static string ResolveCurrencyCode(Market market, string? requestedCurrencyCode)
    {
        if (requestedCurrencyCode is not null
            && market.Currencies.Any(x => string.Equals(x.CurrencyCode, requestedCurrencyCode, StringComparison.OrdinalIgnoreCase)))
        {
            return market.Currencies
                .First(x => string.Equals(x.CurrencyCode, requestedCurrencyCode, StringComparison.OrdinalIgnoreCase))
                .CurrencyCode;
        }

        return market.DefaultCurrency;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsActive(string status)
    {
        return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }
}
