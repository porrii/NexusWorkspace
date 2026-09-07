using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Templates;

/// <summary>A subtask node inside a template definition (recursive).</summary>
public sealed record SubTaskNodeDef
{
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<SubTaskNodeDef> Children { get; init; } = [];
}

/// <summary>A task node inside a project template.</summary>
public sealed record TaskNodeDef
{
    public string Title { get; init; } = string.Empty;

    public Priority Priority { get; init; } = Priority.Medium;

    public IReadOnlyList<SubTaskNodeDef> SubTasks { get; init; } = [];

    public IReadOnlyList<string> Checklist { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>Serialised shape of a <see cref="TemplateKind.Project"/> template.</summary>
public sealed record ProjectTemplateDefinition
{
    public string? Description { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<TaskNodeDef> Tasks { get; init; } = [];
}

/// <summary>Serialised shape of a <see cref="TemplateKind.Task"/> template.</summary>
public sealed record TaskTemplateDefinition
{
    public string? Description { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public IReadOnlyList<SubTaskNodeDef> SubTasks { get; init; } = [];

    public IReadOnlyList<string> Checklist { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record TemplateListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public TemplateKind Kind { get; init; }

    public string? Description { get; init; }

    public int TaskCount { get; init; }

    public int UseCount { get; init; }

    public DateTime? LastUsedAtUtc { get; init; }
}
