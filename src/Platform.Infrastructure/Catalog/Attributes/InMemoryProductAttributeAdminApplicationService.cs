using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Attributes.Commands;
using Platform.Application.Catalog.Attributes.Queries;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Attributes;

namespace Platform.Infrastructure.Catalog.Attributes;

public sealed class InMemoryProductAttributeAdminApplicationService : IProductAttributeAdminApplicationService
{
    private readonly IProductAttributeRepository _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InMemoryProductAttributeAdminApplicationService(IProductAttributeRepository attributeRepository, IUnitOfWork unitOfWork)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ProductAttributeSummaryDto>> ListAsync(ListProductAttributesQuery query, CancellationToken cancellationToken)
    {
        var result = await _attributeRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        return new PagedResponse<ProductAttributeSummaryDto>(result.Items.Select(MapSummary).ToList(), result.Total, page, pageSize);
    }

    public async Task<IReadOnlyList<ProductAttributeEditorDefinitionDto>> ListEditorDefinitionsAsync(ListProductAttributeEditorDefinitionsQuery query, CancellationToken cancellationToken)
    {
        var attributes = await _attributeRepository.ListEditorDefinitionsAsync(query, cancellationToken);
        return attributes.Select(MapEditorDefinition).ToList();
    }

    public async Task<ProductAttributeDetailsDto?> GetByIdAsync(GetProductAttributeByIdQuery query, CancellationToken cancellationToken)
    {
        var attribute = await _attributeRepository.GetByIdAsync(query.AttributeId, cancellationToken);
        return attribute is null ? null : MapDetails(attribute);
    }

    public async Task<ProductAttributeDetailsDto> CreateAsync(CreateProductAttributeCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeIsUniqueAsync(command.Code, null, cancellationToken);

        var now = DateTime.UtcNow;
        var attribute = new ProductAttribute(
            Guid.NewGuid(),
            command.Code,
            command.Name,
            command.Scope,
            command.DataType,
            command.IsVariantDefining,
            command.IsFilterable,
            command.IsRequired,
            command.SortOrder,
            now,
            now,
            MapOptions(command.Options));

        await _attributeRepository.AddAsync(attribute, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(attribute);
    }

    public async Task<ProductAttributeDetailsDto?> UpdateAsync(UpdateProductAttributeCommand command, CancellationToken cancellationToken)
    {
        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        await EnsureCodeIsUniqueAsync(command.Code, command.AttributeId, cancellationToken);

        attribute.Update(
            command.Code,
            command.Name,
            command.Scope,
            command.DataType,
            command.IsVariantDefining,
            command.IsFilterable,
            command.IsRequired,
            command.SortOrder,
            MapOptions(command.Options),
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(attribute);
    }

    public async Task<ProductAttributeDetailsDto?> ArchiveAsync(ArchiveProductAttributeCommand command, CancellationToken cancellationToken)
    {
        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        attribute.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(attribute);
    }

    private async Task EnsureCodeIsUniqueAsync(string code, Guid? currentAttributeId, CancellationToken cancellationToken)
    {
        var existing = await _attributeRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != currentAttributeId)
        {
            throw new ConflictException("Attribute code already exists.");
        }
    }

    private static IReadOnlyList<AttributeOption> MapOptions(IReadOnlyList<UpsertAttributeOptionCommand> commands)
    {
        return commands
            .Select(option => new AttributeOption(option.Id ?? Guid.NewGuid(), option.Code, option.Value, option.SortOrder))
            .ToList();
    }

    private static ProductAttributeSummaryDto MapSummary(ProductAttribute attribute)
    {
        return new ProductAttributeSummaryDto(
            attribute.Id,
            attribute.Code,
            attribute.Name,
            attribute.Scope,
            attribute.DataType,
            attribute.IsVariantDefining,
            attribute.IsFilterable,
            attribute.IsRequired,
            attribute.Options.Count,
            attribute.Status,
            attribute.UpdatedAtUtc,
            attribute.RowVersion);
    }

    private static ProductAttributeDetailsDto MapDetails(ProductAttribute attribute)
    {
        return new ProductAttributeDetailsDto(
            attribute.Id,
            attribute.Code,
            attribute.Name,
            attribute.Scope,
            attribute.DataType,
            attribute.IsVariantDefining,
            attribute.IsFilterable,
            attribute.IsRequired,
            attribute.SortOrder,
            attribute.Status,
            attribute.Options.Select(MapOption).ToList(),
            attribute.CreatedAtUtc,
            attribute.UpdatedAtUtc,
            attribute.RowVersion);
    }

    private static ProductAttributeEditorDefinitionDto MapEditorDefinition(ProductAttribute attribute)
    {
        return new ProductAttributeEditorDefinitionDto(
            attribute.Id,
            attribute.Code,
            attribute.Name,
            attribute.Scope,
            attribute.DataType,
            attribute.IsRequired,
            attribute.Options.Select(MapOption).ToList());
    }

    private static AttributeOptionDto MapOption(AttributeOption option)
    {
        return new AttributeOptionDto(option.Id, option.Code, option.Value, option.SortOrder);
    }
}
