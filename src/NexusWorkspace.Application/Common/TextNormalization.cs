using System.Globalization;
using System.Text;

namespace NexusWorkspace.Application.Common;

/// <summary>Shared text folding for accent-insensitive matching (parser, search, palette).</summary>
public static class TextNormalization
{
    /// <summary>Lower-cases and strips diacritics: "Cartografía" → "cartografia".</summary>
    public static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>True when every character of <paramref name="pattern"/> appears in order in <paramref name="text"/>.</summary>
    public static bool FuzzyMatch(string? text, string? pattern)
    {
        var foldedText = Fold(text);
        var foldedPattern = Fold(pattern);

        if (foldedPattern.Length == 0)
        {
            return true;
        }

        var patternIndex = 0;
        foreach (var ch in foldedText)
        {
            if (ch == foldedPattern[patternIndex] && ++patternIndex == foldedPattern.Length)
            {
                return true;
            }
        }

        return false;
    }
}
