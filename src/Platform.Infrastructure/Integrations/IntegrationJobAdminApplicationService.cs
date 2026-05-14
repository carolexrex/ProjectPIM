using System.Text.Json;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Security;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class IntegrationJobAdminApplicationService : IIntegrationJobAdminApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IIntegrationJobRepository _integrationJobRepository;
    private readonly ICurrentActorAccessor _currentActorAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public IntegrationJobAdminApplicationService(
        IIntegrationJobRepository integrationJobRepository,
        ICurrentActorAccessor currentActorAccessor,
        IUnitOfWork unitOfWork)
    {
        _integrationJobRepository = integrationJobRepository;
        _currentActorAccessor = currentActorAccessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<IntegrationJobSummaryDto>> ListAsync(ListIntegrationJobsQuery query, CancellationToken cancellationToken)
    {
        var result = await _integrationJobRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<IntegrationJobSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<IntegrationJobDetailsDto?> GetByIdAsync(GetIntegrationJobByIdQuery query, CancellationToken cancellationToken)
    {
        var job = await _integrationJobRepository.GetByIdAsync(query.IntegrationJobId, cancellationToken);
        return job is null ? null : MapDetails(job);
    }

    public async Task<IntegrationJobDetailsDto> CreateBrandExportAsync(CreateBrandExportJobCommand command, CancellationToken cancellationToken)
    {
        return await CreateJobAsync(
            IntegrationJobTypes.BrandExport,
            IntegrationJobDirections.Export,
            new BrandExportJobPayload(command.Search, command.Status),
            cancellationToken);
    }

    public async Task<IntegrationJobDetailsDto> CreateBrandImportAsync(CreateBrandImportJobCommand command, CancellationToken cancellationToken)
    {
        if (command.Brands.Count == 0)
        {
            throw new RequestValidationException(nameof(command.Brands), "At least one brand row is required.");
        }

        var payload = new BrandImportJobPayload(
            command.Brands.Select(
                brand => new BrandImportJobPayloadItem(
                    brand.Code,
                    brand.WebsiteUrl,
                    brand.LogoMediaAssetId,
                    brand.SortOrder,
                    brand.Translations.Select(
                        translation => new BrandImportJobPayloadTranslation(
                            translation.CultureCode,
                            translation.Name,
                            translation.Slug,
                            translation.Description))
                        .ToList()))
                .ToList());

        return await CreateJobAsync(
            IntegrationJobTypes.BrandImport,
            IntegrationJobDirections.Import,
            payload,
            cancellationToken);
    }

    public async Task<IntegrationJobDetailsDto> CreateProductExportAsync(CreateProductExportJobCommand command, CancellationToken cancellationToken)
    {
        return await CreateJobAsync(
            IntegrationJobTypes.ProductExport,
            IntegrationJobDirections.Export,
            new ProductExportJobPayload(
                command.Search,
                command.Status,
                command.ProductStatusCode,
                command.BrandId,
                command.HasVariants),
            cancellationToken);
    }

    public async Task<IntegrationJobDetailsDto> CreateProductImportAsync(CreateProductImportJobCommand command, CancellationToken cancellationToken)
    {
        if (command.Products.Count == 0)
        {
            throw new RequestValidationException(nameof(command.Products), "At least one product row is required.");
        }

        var payload = new ProductImportJobPayload(
            command.Products.Select(
                product => new ProductImportJobPayloadItem(
                    product.ProductType,
                    product.ProductNumber,
                    product.Slug,
                    product.BrandCode,
                    product.ProductStatusCode,
                    product.TaxCategoryCode,
                    product.UnitOfMeasure,
                    product.HasVariants,
                    product.Weight,
                    product.Length,
                    product.Width,
                    product.Height,
                    product.CategoryCodes.ToList(),
                    product.AttributeValues.Select(
                        value => new ProductImportJobPayloadAttributeValue(
                            value.ProductAttributeCode,
                            value.AttributeOptionCode,
                            value.ValueText))
                        .ToList(),
                    product.Translations.Select(
                        translation => new ProductImportJobPayloadTranslation(
                            translation.CultureCode,
                            translation.Name,
                            translation.ShortDescription,
                            translation.LongDescription,
                            translation.SeoTitle,
                            translation.SeoDescription))
                        .ToList()))
                .ToList());

        return await CreateJobAsync(
            IntegrationJobTypes.ProductImport,
            IntegrationJobDirections.Import,
            payload,
            cancellationToken);
    }

    public async Task<IntegrationJobDetailsDto> CreateStorefrontProjectionRebuildAsync(CreateStorefrontProjectionRebuildJobCommand command, CancellationToken cancellationToken)
    {
        return await CreateJobAsync(
            IntegrationJobTypes.StorefrontProjectionRebuild,
            IntegrationJobDirections.Rebuild,
            new StorefrontProjectionRebuildJobPayload(),
            cancellationToken);
    }

    private async Task<IntegrationJobDetailsDto> CreateJobAsync(
        string type,
        string direction,
        object payload,
        CancellationToken cancellationToken)
    {
        var actor = _currentActorAccessor.GetCurrentActor();
        var job = new IntegrationJob(
            Guid.NewGuid(),
            type,
            direction,
            string.IsNullOrWhiteSpace(actor.Identifier) ? "system" : actor.Identifier,
            JsonSerializer.Serialize(payload, JsonOptions),
            DateTime.UtcNow);

        await _integrationJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(job);
    }

    private static IntegrationJobSummaryDto MapSummary(IntegrationJob job)
    {
        return new IntegrationJobSummaryDto(
            job.Id,
            job.Type,
            job.Direction,
            job.Status,
            job.RequestedBy,
            job.AttemptCount,
            job.ResultSummary,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            job.RowVersion);
    }

    private static IntegrationJobDetailsDto MapDetails(IntegrationJob job)
    {
        return new IntegrationJobDetailsDto(
            job.Id,
            job.Type,
            job.Direction,
            job.Status,
            job.RequestedBy,
            job.PayloadJson,
            job.ResultPayloadJson,
            job.ResultSummary,
            job.LastError,
            job.AttemptCount,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.NextAttemptAtUtc,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            job.RowVersion);
    }
}
