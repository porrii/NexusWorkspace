using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Domain.Projects;

/// <summary>
/// A unit of work that groups tasks, follow-ups, communications, files, people
/// and companies, and keeps its full history forever.
/// </summary>
public class Project : AuditableEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Material icon key, e.g. "Server", "Camera", "FileDocument".</summary>
    public string? Icon { get; set; }

    /// <summary>Hex colour, e.g. "#4A43D9".</summary>
    public string? Color { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public Priority Priority { get; set; } = Priority.Medium;

    public DateTime? StartDateUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime? CompletedDateUtc { get; set; }

    public Guid? OwnerPersonId { get; set; }

    public Person? Owner { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime? LastOpenedAtUtc { get; set; }

    public ICollection<WorkTask> Tasks { get; } = new List<WorkTask>();

    public ICollection<ProjectPerson> People { get; } = new List<ProjectPerson>();

    public ICollection<ProjectCompany> Companies { get; } = new List<ProjectCompany>();

    public ICollection<ProjectTag> Tags { get; } = new List<ProjectTag>();

    public int TaskProgressPercent
    {
        get
        {
            var relevant = Tasks.Where(t => !t.IsDeleted).ToList();
            if (relevant.Count == 0)
            {
                return 0;
            }

            var done = relevant.Count(t => t.Status == WorkTaskStatus.Finished);
            return (int)Math.Round(100.0 * done / relevant.Count);
        }
    }
}
