using System.Text.Json;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Application.Import;

/// <summary>
/// Parses free-form JSON or indented notes into a reviewable <see cref="ImportPreview"/>,
/// then commits it as real projects/tasks. Everything imported logs
/// <see cref="ActivityType.Imported"/> history.
/// </summary>
public sealed class ImportService(IApplicationDbContext db, IActivityLog activity)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Result<ImportPreview> Parse(ImportSource source, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Failure<ImportPreview>("import.empty", "No hay nada que importar.");
        }

        try
        {
            var preview = source == ImportSource.Json ? ParseJson(input) : ParseNotes(input);
            if (preview.ProjectCount == 0)
            {
                return Result.Failure<ImportPreview>("import.nothing", "No se ha reconocido ningún proyecto o tarea.");
            }

            return preview;
        }
        catch (JsonException ex)
        {
            return Result.Failure<ImportPreview>("import.bad_json", $"JSON no válido: {ex.Message}");
        }
    }

    public async Task<Result<int>> CommitAsync(ImportPreview preview, CancellationToken cancellationToken = default)
    {
        if (preview.ProjectCount == 0)
        {
            return Result.Failure<int>("import.nothing", "Nada que importar.");
        }

        foreach (var projectNode in preview.Projects)
        {
            var project = new Project
            {
                Name = string.IsNullOrWhiteSpace(projectNode.Name) ? "Proyecto importado" : projectNode.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(projectNode.Description) ? null : projectNode.Description.Trim(),
                Status = ProjectStatus.Planning,
            };
            db.Projects.Add(project);

            var sortKey = 1d;
            foreach (var taskNode in projectNode.Tasks)
            {
                AddTask(project.Id, taskNode, ref sortKey);
            }

            activity.Record(EntityKind.Project, project.Id, ActivityType.Imported,
                $"Proyecto «{project.Name}» importado ({CountTasks(projectNode.Tasks)} tareas).", project.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
        return preview.ProjectCount;

        void AddTask(Guid projectId, ImportTaskNode node, ref double sortKey)
        {
            var task = new WorkTask
            {
                ProjectId = projectId,
                Title = string.IsNullOrWhiteSpace(node.Title) ? "(sin título)" : node.Title.Trim(),
                Priority = node.Priority,
                SortKey = sortKey++,
            };
            db.WorkTasks.Add(task);

            var checkKey = 1d;
            foreach (var text in node.Checklist.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                db.ChecklistItems.Add(new ChecklistItem { WorkTaskId = task.Id, Text = text.Trim(), SortKey = checkKey++ });
            }

            AddSubTasks(task.Id, node.SubTasks, null);
        }

        void AddSubTasks(Guid taskId, IReadOnlyList<ImportTaskNode> nodes, Guid? parentId)
        {
            var key = 1d;
            foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Title)))
            {
                var sub = new SubTask
                {
                    WorkTaskId = taskId,
                    ParentSubTaskId = parentId,
                    Title = node.Title.Trim(),
                    SortKey = key++,
                };
                db.SubTasks.Add(sub);
                AddSubTasks(taskId, node.SubTasks, sub.Id);
            }
        }
    }

    private static ImportPreview ParseJson(string input)
    {
        using var doc = JsonDocument.Parse(input);
        var root = doc.RootElement;
        var projectsElement = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("projects", out var p) ? p : default;

        var preview = new ImportPreview();
        if (projectsElement.ValueKind != JsonValueKind.Array)
        {
            preview.Warnings.Add("Se esperaba un array 'projects' o un array de proyectos en la raíz.");
            return preview;
        }

        foreach (var projectEl in projectsElement.EnumerateArray())
        {
            var node = new ImportProjectNode
            {
                Name = GetString(projectEl, "name") ?? "Proyecto importado",
                Description = GetString(projectEl, "description"),
            };

            if (projectEl.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var taskEl in tasksEl.EnumerateArray())
                {
                    node.Tasks.Add(ReadTask(taskEl));
                }
            }

            preview.Projects.Add(node);
        }

        return preview;
    }

    private static ImportTaskNode ReadTask(JsonElement el)
    {
        var task = new ImportTaskNode
        {
            Title = GetString(el, "title") ?? GetString(el, "name") ?? "(sin título)",
            Priority = ParsePriority(GetString(el, "priority")),
        };

        if (el.TryGetProperty("checklist", out var checkEl) && checkEl.ValueKind == JsonValueKind.Array)
        {
            task.Checklist.AddRange(checkEl.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        foreach (var name in new[] { "subTasks", "subtasks", "children" })
        {
            if (el.TryGetProperty(name, out var subEl) && subEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in subEl.EnumerateArray())
                {
                    task.SubTasks.Add(ReadTask(child));
                }

                break;
            }
        }

        return task;
    }

    private static ImportPreview ParseNotes(string input)
    {
        var preview = new ImportPreview();
        ImportProjectNode? currentProject = null;
        var stack = new List<(int Indent, ImportTaskNode Node)>();

        foreach (var rawLine in input.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var expanded = rawLine.Replace("\t", "    ");
            var indent = expanded.Length - expanded.TrimStart().Length;
            var text = expanded.Trim();

            var isChecklist = text.StartsWith("- [", StringComparison.Ordinal)
                              || text.StartsWith("* ", StringComparison.Ordinal)
                              || text.StartsWith("- ", StringComparison.Ordinal);

            var isProject = text.StartsWith("# ", StringComparison.Ordinal)
                            || (indent == 0 && currentProject is null);

            if (isProject && !isChecklist)
            {
                currentProject = new ImportProjectNode { Name = Strip(text, "# ") };
                preview.Projects.Add(currentProject);
                stack.Clear();
                continue;
            }

            currentProject ??= NewDefaultProject(preview);

            if (isChecklist)
            {
                while (stack.Count > 0 && stack[^1].Indent >= indent)
                {
                    stack.RemoveAt(stack.Count - 1);
                }

                if (stack.Count > 0)
                {
                    stack[^1].Node.Checklist.Add(StripChecklist(text));
                }
                else if (currentProject.Tasks.Count > 0)
                {
                    currentProject.Tasks[^1].Checklist.Add(StripChecklist(text));
                }
                else
                {
                    preview.Warnings.Add($"Ítem de checklist sin tarea: «{StripChecklist(text)}»");
                }

                continue;
            }

            var (title, priority) = ReadInlinePriority(Strip(text, "- ", "* "));
            var node = new ImportTaskNode { Title = title, Priority = priority };

            while (stack.Count > 0 && stack[^1].Indent >= indent)
            {
                stack.RemoveAt(stack.Count - 1);
            }

            if (stack.Count == 0)
            {
                currentProject.Tasks.Add(node);
            }
            else
            {
                stack[^1].Node.SubTasks.Add(node);
            }

            stack.Add((indent, node));
        }

        return preview;

        static ImportProjectNode NewDefaultProject(ImportPreview p)
        {
            var node = new ImportProjectNode { Name = "Notas importadas" };
            p.Projects.Add(node);
            return node;
        }
    }

    private static (string Title, Priority Priority) ReadInlinePriority(string text)
    {
        var trimmed = text.TrimEnd();
        if (trimmed.EndsWith("!!", StringComparison.Ordinal))
        {
            return (trimmed[..^2].TrimEnd(), Priority.Critical);
        }

        if (trimmed.EndsWith('!'))
        {
            return (trimmed[..^1].TrimEnd(), Priority.High);
        }

        if (trimmed.EndsWith('?'))
        {
            return (trimmed[..^1].TrimEnd(), Priority.Low);
        }

        return (trimmed, Priority.Medium);
    }

    private static string Strip(string text, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.Ordinal))
            {
                return text[prefix.Length..].Trim();
            }
        }

        return text;
    }

    private static string StripChecklist(string text)
    {
        if (text.StartsWith("- [", StringComparison.Ordinal))
        {
            var close = text.IndexOf(']');
            return close >= 0 ? text[(close + 1)..].Trim() : text;
        }

        return Strip(text, "- ", "* ");
    }

    private static string? GetString(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object
           && el.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static Priority ParsePriority(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "critical" or "crítica" or "critica" => Priority.Critical,
        "high" or "alta" => Priority.High,
        "low" or "baja" => Priority.Low,
        _ => Priority.Medium,
    };

    private static int CountTasks(IReadOnlyList<ImportTaskNode> tasks)
        => tasks.Count + tasks.Sum(t => CountTasks(t.SubTasks));
}
