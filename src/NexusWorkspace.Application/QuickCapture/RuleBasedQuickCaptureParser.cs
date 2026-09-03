using System.Globalization;
using System.Text;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.QuickCapture;

/// <summary>
/// Deterministic, offline quick-capture parser. Recognises <c>#tag</c>,
/// <c>@persona</c>, <c>!prioridad</c>, Spanish date words ("hoy", "mañana",
/// "lunes"…), <c>dd/MM[/yyyy]</c>, a leading action verb and a known project name.
/// Never guesses destructively — it only fills in suggestions.
/// </summary>
public sealed class RuleBasedQuickCaptureParser(IClock clock) : IQuickCaptureParser
{
    private static readonly HashSet<string> ActionVerbs = new(StringComparer.Ordinal)
    {
        "enviar", "mandar", "revisar", "mirar", "comprobar", "llamar", "telefonear",
        "contactar", "preparar", "montar", "documentar", "escribir", "probar", "testear",
        "desplegar", "publicar", "actualizar", "instalar", "configurar", "cerrar", "abrir",
        "crear", "planificar", "coordinar", "validar", "verificar", "recordar", "avisar",
        "solicitar", "pedir", "responder", "contestar", "revisa", "envia", "llama",
    };

    private static readonly Dictionary<string, DayOfWeek> Weekdays = new(StringComparer.Ordinal)
    {
        ["lunes"] = DayOfWeek.Monday,
        ["martes"] = DayOfWeek.Tuesday,
        ["miercoles"] = DayOfWeek.Wednesday,
        ["jueves"] = DayOfWeek.Thursday,
        ["viernes"] = DayOfWeek.Friday,
        ["sabado"] = DayOfWeek.Saturday,
        ["domingo"] = DayOfWeek.Sunday,
    };

    public QuickCaptureParseResult Parse(string input, IReadOnlyCollection<ProjectNameRef>? knownProjects = null)
    {
        input ??= string.Empty;
        var tokens = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var tags = new List<string>();
        var people = new List<string>();
        Priority? priority = null;
        var kept = new List<string>();

        foreach (var token in tokens)
        {
            if (token.Length > 1 && token[0] == '#')
            {
                tags.Add(token[1..].Trim('#', ',', '.', ';'));
            }
            else if (token.Length > 1 && token[0] == '@')
            {
                people.Add(token[1..].Trim('@', ',', '.', ';'));
            }
            else if (token.Length > 1 && token[0] == '!')
            {
                priority ??= PriorityFromToken(token[1..]);
            }
            else
            {
                kept.Add(token);
            }
        }

        var cleanText = string.Join(' ', kept).Trim();
        var normalized = RemoveDiacritics(input).ToLowerInvariant();

        var (projectId, projectName) = MatchProject(normalized, knownProjects);
        var dueDate = MatchDate(normalized);
        var action = MatchAction(kept);

        return new QuickCaptureParseResult
        {
            RawText = input,
            CleanText = cleanText.Length == 0 ? input.Trim() : cleanText,
            ProjectId = projectId,
            ProjectName = projectName,
            Priority = priority,
            DueDateUtc = dueDate,
            Action = action,
            Tags = tags.Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            People = people.Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
        };
    }

    private static Priority? PriorityFromToken(string value) => RemoveDiacritics(value).ToLowerInvariant() switch
    {
        "critica" or "crit" or "c" or "!" => Priority.Critical,
        "alta" or "a" or "high" => Priority.High,
        "media" or "m" or "normal" => Priority.Medium,
        "baja" or "b" or "low" => Priority.Low,
        _ => null,
    };

    private static (Guid? Id, string? Name) MatchProject(string normalizedInput, IReadOnlyCollection<ProjectNameRef>? projects)
    {
        if (projects is null || projects.Count == 0)
        {
            return (null, null);
        }

        ProjectNameRef? best = null;
        var bestLength = 0;

        foreach (var project in projects)
        {
            var name = RemoveDiacritics(project.Name).ToLowerInvariant().Trim();
            if (name.Length < 3)
            {
                continue;
            }

            if (ContainsWord(normalizedInput, name) && name.Length > bestLength)
            {
                best = project;
                bestLength = name.Length;
            }
        }

        return best is null ? (null, null) : (best.Id, best.Name);
    }

    private DateTime? MatchDate(string normalizedInput)
    {
        var today = clock.UtcNow.Date;

        if (ContainsWord(normalizedInput, "pasado manana"))
        {
            return today.AddDays(2);
        }

        if (ContainsWord(normalizedInput, "manana"))
        {
            return today.AddDays(1);
        }

        if (ContainsWord(normalizedInput, "hoy"))
        {
            return today;
        }

        if (ContainsWord(normalizedInput, "finde") || ContainsWord(normalizedInput, "fin de semana"))
        {
            return NextWeekday(today, DayOfWeek.Saturday);
        }

        foreach (var (word, day) in Weekdays)
        {
            if (ContainsWord(normalizedInput, word))
            {
                return NextWeekday(today, day);
            }
        }

        foreach (var token in normalizedInput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseDayMonth(token, today, out var date))
            {
                return date;
            }
        }

        return null;
    }

    private static bool TryParseDayMonth(string token, DateTime today, out DateTime date)
    {
        date = default;

        var parts = token
            .Trim('.', ',', ';', ')', '(')
            .Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is < 2 or > 3
            || !int.TryParse(parts[0], out var day)
            || !int.TryParse(parts[1], out var month)
            || day is < 1 or > 31
            || month is < 1 or > 12)
        {
            return false;
        }

        try
        {
            if (parts.Length == 3 && int.TryParse(parts[2], out var year))
            {
                date = new DateTime(year < 100 ? 2000 + year : year, month, day);
                return true;
            }

            // No year given: this year, or next year if the date has already passed.
            var candidate = new DateTime(today.Year, month, day);
            date = candidate.Date < today ? candidate.AddYears(1) : candidate;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static DateTime NextWeekday(DateTime from, DayOfWeek target)
    {
        var delta = ((int)target - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(delta == 0 ? 7 : delta);
    }

    private static string? MatchAction(IReadOnlyList<string> keptTokens)
    {
        if (keptTokens.Count == 0)
        {
            return null;
        }

        var first = RemoveDiacritics(keptTokens[0]).ToLowerInvariant().Trim('.', ',', ':');
        if (!ActionVerbs.Contains(first))
        {
            return null;
        }

        return char.ToUpperInvariant(keptTokens[0][0]) + keptTokens[0][1..].ToLowerInvariant();
    }

    private static bool ContainsWord(string haystack, string needle)
    {
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            var before = index == 0 || !char.IsLetterOrDigit(haystack[index - 1]);
            var afterIndex = index + needle.Length;
            var after = afterIndex >= haystack.Length || !char.IsLetterOrDigit(haystack[afterIndex]);
            if (before && after)
            {
                return true;
            }

            index = haystack.IndexOf(needle, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
