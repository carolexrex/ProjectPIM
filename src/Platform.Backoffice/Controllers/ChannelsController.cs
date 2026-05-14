using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Channels;
using Platform.Contracts.Catalog.Markets;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("channels")]
public sealed class ChannelsController : Controller
{
    private readonly IAdminApiClient _apiClient;

    public ChannelsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListChannelsAsync(search, status, "code", cancellationToken);

        return View(new ChannelListPageViewModel
        {
            Search = search,
            Status = status,
            Channels = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    public IActionResult New()
    {
        return View(new ChannelCreateViewModel());
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(ChannelCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateChannelAsync(
                new CreateChannelRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    HostName = form.HostName
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Channel {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
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
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Channel")] ChannelUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, channelForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateChannelAsync(
                id,
                new UpdateChannelRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    HostName = form.HostName,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Channel {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Channel");
            var invalidPage = await BuildDetailsPageAsync(id, channelForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var archived = await _apiClient.ArchiveChannelAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Channel {archived.Code} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/markets")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertMarketAssignment(
        Guid id,
        [Bind(Prefix = "MarketAssignmentForm")] ChannelMarketAssignmentCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, marketAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertChannelMarketAssignmentAsync(
                id,
                new UpsertChannelMarketAssignmentRequest
                {
                    MarketId = form.MarketId!.Value,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Channel market assignment saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "MarketAssignmentForm");
            var invalidPage = await BuildDetailsPageAsync(id, marketAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/markets/{marketId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMarketAssignment(Guid id, Guid marketId, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemoveChannelMarketAssignmentAsync(
                id,
                marketId,
                new RemoveChannelMarketAssignmentRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Channel market assignment removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<ChannelDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid channelId,
        ChannelUpdateViewModel? channelForm = null,
        ChannelMarketAssignmentCreateViewModel? marketAssignmentForm = null,
        CancellationToken cancellationToken = default)
    {
        var channelTask = _apiClient.GetChannelAsync(channelId, cancellationToken);
        var activeMarketsTask = _apiClient.ListMarketsAsync(null, "Active", "code", cancellationToken);

        await Task.WhenAll(channelTask, activeMarketsTask);

        var channel = await channelTask;
        if (channel is null)
        {
            return null;
        }

        channelForm ??= new ChannelUpdateViewModel
        {
            Id = channel.Id,
            Code = channel.Code,
            Name = channel.Name,
            HostName = channel.HostName,
            RowVersion = channel.RowVersion,
            Status = channel.Status,
            CreatedAtUtc = channel.CreatedAtUtc,
            UpdatedAtUtc = channel.UpdatedAtUtc
        };

        marketAssignmentForm ??= new ChannelMarketAssignmentCreateViewModel
        {
            ChannelId = channel.Id,
            RowVersion = channel.RowVersion
        };
        marketAssignmentForm.MarketOptions = BuildMarketOptions(channel.Markets, (await activeMarketsTask).Items);

        return new ChannelDetailsPageViewModel
        {
            Channel = channelForm,
            Markets = channel.Markets,
            MarketAssignmentForm = marketAssignmentForm
        };
    }

    private static IReadOnlyList<MarketLookupOptionViewModel> BuildMarketOptions(
        IReadOnlyList<ChannelMarketAssignmentDto> assignedMarkets,
        IReadOnlyList<MarketSummaryDto> activeMarkets)
    {
        var assignedIds = assignedMarkets.Select(x => x.MarketId).ToHashSet();

        return activeMarkets
            .Where(x => !assignedIds.Contains(x.Id))
            .Select(x => new MarketLookupOptionViewModel(
                x.Id,
                x.Code,
                $"{x.Name} ({x.Code})"))
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
