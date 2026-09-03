using System.Runtime.CompilerServices;

namespace NexusWorkspace.Domain.Common;

/// <summary>Small guard helpers for domain invariants. Keep intentionally minimal.</summary>
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"'{paramName}' es obligatorio.");
        }

        return value.Trim();
    }

    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
        => value ?? throw new DomainException($"'{paramName}' es obligatorio.");
}
