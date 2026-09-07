using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Templates;

/// <summary>
/// A reusable tree: a whole project (tasks → subtasks → checklist + tags) or a
/// single task. The shape is stored as JSON in <see cref="DefinitionJson"/>;
/// the domain treats it as opaque text.
/// </summary>
public class Template : AuditableEntity
{
    public required string Name { get; set; }

    public TemplateKind Kind { get; set; } = TemplateKind.Project;

    public string? Description { get; set; }

    /// <summary>Serialised <c>ProjectTemplateDefinition</c> / <c>TaskTemplateDefinition</c>.</summary>
    public required string DefinitionJson { get; set; }

    public int UseCount { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }
}
