using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.UI.ViewModels.Shared;

/// <summary>Inline "log a communication" form, reused by the person / company / project / task detail screens.</summary>
public partial class CommunicationComposerViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private CommunicationChannel _channel = CommunicationChannel.Email;

    [ObservableProperty]
    private CommunicationDirection _direction = CommunicationDirection.Outbound;

    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _body = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _when = DateTimeOffset.Now;

    public IReadOnlyList<CommunicationChannel> Channels { get; } = Enum.GetValues<CommunicationChannel>();

    public IReadOnlyList<CommunicationDirection> Directions { get; } = Enum.GetValues<CommunicationDirection>();

    public void Reset()
    {
        IsOpen = false;
        Channel = CommunicationChannel.Email;
        Direction = CommunicationDirection.Outbound;
        Subject = string.Empty;
        Body = string.Empty;
        When = DateTimeOffset.Now;
    }
}

/// <summary>Inline "schedule a meeting" form.</summary>
public partial class MeetingComposerViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _agenda = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _date = DateTimeOffset.Now.Date.AddDays(1);

    [ObservableProperty]
    private string _time = "10:00";

    [ObservableProperty]
    private int _durationMinutes = 60;

    [ObservableProperty]
    private string _location = string.Empty;

    public void Reset()
    {
        IsOpen = false;
        Title = string.Empty;
        Agenda = string.Empty;
        Date = DateTimeOffset.Now.Date.AddDays(1);
        Time = "10:00";
        DurationMinutes = 60;
        Location = string.Empty;
    }

    public DateTime StartUtc()
    {
        var day = (Date ?? DateTimeOffset.Now).Date;
        var minutes = 10 * 60;
        if (TimeSpan.TryParse(Time, out var parsed))
        {
            minutes = (int)parsed.TotalMinutes;
        }

        return DateTime.SpecifyKind(day.AddMinutes(minutes), DateTimeKind.Local).ToUniversalTime();
    }
}

public sealed record RelationTargetHit(EntityKind Kind, Guid Id, string Label, Guid? ProjectId);

/// <summary>Inline "add a cross-reference" form: pick a kind, search, choose a match, set the relation type.</summary>
public partial class RelationComposerViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private EntityKind _targetKind = EntityKind.Project;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private RelationKind _kind = RelationKind.RelatesTo;

    [ObservableProperty]
    private string _note = string.Empty;

    public ObservableCollection<RelationTargetHit> Results { get; } = [];

    public IReadOnlyList<EntityKind> TargetKinds { get; } =
        [EntityKind.Project, EntityKind.WorkTask, EntityKind.Person, EntityKind.Company];

    public IReadOnlyList<RelationKind> RelationKinds { get; } = Enum.GetValues<RelationKind>();

    public void Reset()
    {
        IsOpen = false;
        TargetKind = EntityKind.Project;
        SearchText = string.Empty;
        Kind = RelationKind.RelatesTo;
        Note = string.Empty;
        Results.Clear();
    }

    public async Task<IReadOnlyList<RelationTargetHit>> SearchAsync(IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<IApplicationDbContext>();
        var term = (SearchText ?? string.Empty).Trim();
        if (term.Length < 2)
        {
            return [];
        }

        var like = $"%{term}%";
        return TargetKind switch
        {
            EntityKind.Project => await db.Projects.AsNoTracking()
                .Where(p => EF.Functions.Like(p.Name, like))
                .OrderBy(p => p.Name).Take(15)
                .Select(p => new RelationTargetHit(EntityKind.Project, p.Id, p.Name, p.Id))
                .ToListAsync(ct),
            EntityKind.WorkTask => await db.WorkTasks.AsNoTracking()
                .Where(t => EF.Functions.Like(t.Title, like))
                .OrderBy(t => t.Title).Take(15)
                .Select(t => new RelationTargetHit(EntityKind.WorkTask, t.Id, t.Title, t.ProjectId))
                .ToListAsync(ct),
            EntityKind.Person => await db.People.AsNoTracking()
                .Where(p => EF.Functions.Like(p.Name, like))
                .OrderBy(p => p.Name).Take(15)
                .Select(p => new RelationTargetHit(EntityKind.Person, p.Id, p.Name, (Guid?)null))
                .ToListAsync(ct),
            EntityKind.Company => await db.Companies.AsNoTracking()
                .Where(c => EF.Functions.Like(c.Name, like))
                .OrderBy(c => c.Name).Take(15)
                .Select(c => new RelationTargetHit(EntityKind.Company, c.Id, c.Name, (Guid?)null))
                .ToListAsync(ct),
            _ => [],
        };
    }
}
