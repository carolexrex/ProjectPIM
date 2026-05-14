using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;

namespace Platform.Application.Integrations;

public interface IIntegrationJobAdminApplicationService
{
    Task<PagedResponse<IntegrationJobSummaryDto>> ListAsync(ListIntegrationJobsQuery query, CancellationToken cancellationToken);
    Task<IntegrationJobDetailsDto?> GetByIdAsync(GetIntegrationJobByIdQuery query, CancellationToken cancellationToken);
    Task<IntegrationJobDetailsDto> CreateBrandExportAsync(CreateBrandExportJobCommand command, CancellationToken cancellationToken);
    Task<IntegrationJobDetailsDto> CreateBrandImportAsync(CreateBrandImportJobCommand command, CancellationToken cancellationToken);
    Task<IntegrationJobDetailsDto> CreateProductExportAsync(CreateProductExportJobCommand command, CancellationToken cancellationToken);
    Task<IntegrationJobDetailsDto> CreateProductImportAsync(CreateProductImportJobCommand command, CancellationToken cancellationToken);
    Task<IntegrationJobDetailsDto> CreateStorefrontProjectionRebuildAsync(CreateStorefrontProjectionRebuildJobCommand command, CancellationToken cancellationToken);
}
