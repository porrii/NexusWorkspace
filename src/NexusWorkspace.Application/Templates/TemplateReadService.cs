using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Templates;

public sealed class TemplateReadService(IApplicationDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TemplateListItem>> GetListAsync(TemplateKind? kind = null, CancellationToken cancellationToken = default)
    {
        var query = db.Templates.AsNoTracking().AsQueryable();
        if (kind is { } k)
        {
            query = query.Where(t => t.Kind == k);
        }

        var rows = await query
            .OrderByDescending(t => t.LastUsedAtUtc)
            .ThenBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.Kind, t.Description, t.DefinitionJson, t.UseCount, t.LastUsedAtUtc })
            .ToListAsync(cancellationToken);

        return rows.Select(t => new TemplateListItem
        {
            Id = t.Id,
            Name = t.Name,
            Kind = t.Kind,
            Description = t.Description,
            UseCount = t.UseCount,
            LastUsedAtUtc = t.LastUsedAtUtc,
            TaskCount = CountTasks(t.Kind, t.DefinitionJson),
        }).ToList();
    }

    private static int CountTasks(TemplateKind kind, string definitionJson)
    {
        try
        {
            if (kind == TemplateKind.Project)
            {
                return JsonSerializer.Deserialize<ProjectTemplateDefinition>(definitionJson, Json)?.Tasks.Count ?? 0;
            }

            var def = JsonSerializer.Deserialize<TaskTemplateDefinition>(definitionJson, Json);
            return def is null ? 0 : CountNodes(def.SubTasks);
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static int CountNodes(IReadOnlyList<SubTaskNodeDef> nodes)
        => nodes.Count + nodes.Sum(n => CountNodes(n.Children));
}
