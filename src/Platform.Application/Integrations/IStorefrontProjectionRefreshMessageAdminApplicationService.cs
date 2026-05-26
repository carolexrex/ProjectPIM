using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;

namespace Platform.Application.Integrations;

public interface IStorefrontProjectionRefreshMessageAdminApplicationService
{
    Task<PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>> ListAsync(
        ListStorefrontProjectionRefreshMessagesQuery query,
        CancellationToken cancellationToken);

    Task<StorefrontProjectionRefreshMessageDetailsDto?> GetByIdAsync(
        GetStorefrontProjectionRefreshMessageByIdQuery query,
        CancellationToken cancellationToken);

    Task<StorefrontProjectionRefreshMessageDetailsDto?> ResetAsync(
        ResetStorefrontProjectionRefreshMessageCommand command,
        CancellationToken cancellationToken);
}
