using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Companies;

public sealed record CreateCompanyRequest
{
    public string Name { get; init; } = string.Empty;

    public CompanyKind Kind { get; init; } = CompanyKind.Other;

    public string? Website { get; init; }

    public string? Notes { get; init; }
}

public sealed record UpdateCompanyRequest
{
    public required Guid Id { get; init; }

    public string? Name { get; init; }

    public CompanyKind? Kind { get; init; }

    public string? Website { get; init; }

    public string? Notes { get; init; }
}
