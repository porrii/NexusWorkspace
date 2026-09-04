using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Relations;

/// <summary>Read-side for cross-references, with the "other" endpoint resolved to a label.</summary>
public sealed class RelationReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<RelationView>> GetForEntityAsync(EntityKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        var raw = await db.EntityRelations.AsNoTracking()
            .Where(r => (r.FromKind == kind && r.FromId == id) || (r.ToKind == kind && r.ToId == id))
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new
            {
                r.Id,
                r.Kind,
                r.FromKind,
                r.FromId,
                r.ToKind,
                r.ToId,
                r.Note,
                r.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        if (raw.Count == 0)
        {
            return [];
        }

        var endpoints = raw
            .Select(r => r.FromKind == kind && r.FromId == id ? (r.ToKind, r.ToId) : (r.FromKind, r.FromId))
            .Distinct()
            .ToList();

        var labels = await ResolveLabelsAsync(endpoints, cancellationToken);

        return raw.Select(r =>
        {
            var outgoing = r.FromKind == kind && r.FromId == id;
            var otherKind = outgoing ? r.ToKind : r.FromKind;
            var otherId = outgoing ? r.ToId : r.FromId;
            var info = labels.GetValueOrDefault((otherKind, otherId));
            return new RelationView
            {
                Id = r.Id,
                Kind = r.Kind,
                IsOutgoing = outgoing,
                OtherKind = otherKind,
                OtherId = otherId,
                OtherLabel = info.Label ?? "(eliminado)",
                OtherProjectId = info.ProjectId,
                Note = r.Note,
                CreatedAtUtc = r.CreatedAtUtc,
            };
        }).ToList();
    }

    private async Task<Dictionary<(EntityKind, Guid), (string? Label, Guid? ProjectId)>> ResolveLabelsAsync(
        IReadOnlyList<(EntityKind Kind, Guid Id)> endpoints,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<(EntityKind, Guid), (string? Label, Guid? ProjectId)>();

        foreach (var group in endpoints.GroupBy(e => e.Kind))
        {
            var ids = group.Select(e => e.Id).ToList();
            switch (group.Key)
            {
                case EntityKind.Project:
                    foreach (var row in await db.Projects.AsNoTracking().IgnoreQueryFilters()
                                 .Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Name }).ToListAsync(cancellationToken))
                    {
                        map[(EntityKind.Project, row.Id)] = (row.Name, row.Id);
                    }

                    break;

                case EntityKind.WorkTask:
                    foreach (var row in await db.WorkTasks.AsNoTracking().IgnoreQueryFilters()
                                 .Where(t => ids.Contains(t.Id)).Select(t => new { t.Id, t.Title, t.ProjectId }).ToListAsync(cancellationToken))
                    {
                        map[(EntityKind.WorkTask, row.Id)] = (row.Title, row.ProjectId);
                    }

                    break;

                case EntityKind.Person:
                    foreach (var row in await db.People.AsNoTracking().IgnoreQueryFilters()
                                 .Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Name }).ToListAsync(cancellationToken))
                    {
                        map[(EntityKind.Person, row.Id)] = (row.Name, null);
                    }

                    break;

                case EntityKind.Company:
                    foreach (var row in await db.Companies.AsNoTracking().IgnoreQueryFilters()
                                 .Where(c => ids.Contains(c.Id)).Select(c => new { c.Id, c.Name }).ToListAsync(cancellationToken))
                    {
                        map[(EntityKind.Company, row.Id)] = (row.Name, null);
                    }

                    break;

                case EntityKind.FollowUp:
                    foreach (var row in await db.FollowUps.AsNoTracking()
                                 .Where(f => ids.Contains(f.Id)).Select(f => new { f.Id, f.Subject, f.ProjectId }).ToListAsync(cancellationToken))
                    {
                        map[(EntityKind.FollowUp, row.Id)] = (row.Subject, row.ProjectId);
                    }

                    break;

                case EntityKind.Meeting:
                    foreach (var row in await db.Meetings.AsNoTracking()
                                 .Where(m => ids.Contains(m.Id)).Select(m => new { m.Id, m.Title, m.ProjectId }).ToListAsync(cancellationToken))
                    {
                        map[(EntityKind.Meeting, row.Id)] = (row.Title, row.ProjectId);
                    }

                    break;
            }
        }

        return map;
    }
}
