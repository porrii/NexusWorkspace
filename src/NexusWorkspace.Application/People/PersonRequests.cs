namespace NexusWorkspace.Application.People;

public sealed record CreatePersonRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Role { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }

    public Guid? CompanyId { get; init; }
}

public sealed record UpdatePersonRequest
{
    public required Guid Id { get; init; }

    public string? Name { get; init; }

    public string? Role { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }

    /// <summary>Pass <see cref="Guid.Empty"/> to clear the company; null leaves it untouched.</summary>
    public Guid? CompanyId { get; init; }
}
