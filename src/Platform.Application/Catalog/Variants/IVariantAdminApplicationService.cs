using Platform.Application.Catalog.Variants.Commands;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Contracts.Catalog.Variants;

namespace Platform.Application.Catalog.Variants;

public interface IVariantAdminApplicationService
{
    Task<IReadOnlyList<VariantSummaryDto>> ListByProductAsync(ListVariantsByProductQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<VariantLookupDto>> ListLookupsAsync(ListVariantLookupsQuery query, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> GetByIdAsync(GetVariantByIdQuery query, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> CreateAsync(CreateVariantCommand command, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> UpdateAsync(UpdateVariantCommand command, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> AssignStatusAsync(AssignVariantStatusCommand command, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> UpsertMediaAsync(UpsertVariantMediaCommand command, CancellationToken cancellationToken);
    Task<VariantDetailsDto?> RemoveMediaAsync(RemoveVariantMediaCommand command, CancellationToken cancellationToken);
}
