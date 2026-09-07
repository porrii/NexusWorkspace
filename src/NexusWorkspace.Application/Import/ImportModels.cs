using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Import;

public enum ImportSource
{
    Json = 0,
    Notes = 1,
    Csv = 2,
}

/// <summary>Maps CSV columns (by header name) onto the import model. A header row is required.</summary>
public sealed class CsvImportMap
{
    public string Delimiter { get; set; } = ",";

    /// <summary>Groups rows into projects. When empty, everything lands in one project.</summary>
    public string? ProjectColumn { get; set; }

    /// <summary>Required: the task title.</summary>
    public string? TaskColumn { get; set; }

    public string? DescriptionColumn { get; set; }

    public string? PriorityColumn { get; set; }

    /// <summary>Checklist items inside one cell, separated by ';' or '|'.</summary>
    public string? ChecklistColumn { get; set; }
}

public sealed class ImportTaskNode
{
    public string Title { get; set; } = string.Empty;

    public Priority Priority { get; set; } = Priority.Medium;

    public List<ImportTaskNode> SubTasks { get; init; } = [];

    public List<string> Checklist { get; init; } = [];
}

public sealed class ImportProjectNode
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<ImportTaskNode> Tasks { get; init; } = [];
}

/// <summary>Result of parsing import input — shown to the user before committing.</summary>
public sealed class ImportPreview
{
    public List<ImportProjectNode> Projects { get; init; } = [];

    public List<string> Warnings { get; init; } = [];

    public int ProjectCount => Projects.Count;

    public int TaskCount => Projects.Sum(p => CountTasks(p.Tasks));

    private static int CountTasks(IReadOnlyList<ImportTaskNode> tasks)
        => tasks.Count + tasks.Sum(t => CountTasks(t.SubTasks));
}
