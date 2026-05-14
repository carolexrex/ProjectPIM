using Platform.Domain.Companies;

namespace Platform.Application.Companies;

public sealed record CompanyListResult(IReadOnlyList<Company> Items, int Total);
