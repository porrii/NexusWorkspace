namespace NexusWorkspace.Application.Common;

/// <summary>
/// Deterministic fallback colours for tags that have no explicit colour. A tag
/// always renders with the same chip colour across every screen.
/// </summary>
public static class TagColors
{
    private static readonly string[] Palette =
    [
        "#4A43D9", "#2E9E63", "#DD8330", "#9A2F3A", "#7A5AD1",
        "#2F7DA3", "#B8951F", "#3E7C4B", "#C2557A", "#5C7CB0",
    ];

    public static string For(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Palette[0];
        }

        var hash = 0;
        foreach (var c in name.Trim().ToLowerInvariant())
        {
            hash = unchecked((hash * 31) + c);
        }

        return Palette[Math.Abs(hash) % Palette.Length];
    }
}
