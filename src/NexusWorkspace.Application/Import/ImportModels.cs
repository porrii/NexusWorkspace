using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Import;

public enum ImportSource
{
    Json = 0,
    Notes = 1,
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
