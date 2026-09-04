using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Domain.Communications;

/// <summary>
/// A logged interaction with a third party — an email, a call, a chat, a note of
/// something said in person. Feeds the aggregated history of the person, the
/// company, the project and the task it touches. Immutable in spirit: edited
/// rarely, never silently deleted (soft-delete only).
/// </summary>
public class Communication : AuditableEntity
{
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Email;

    public CommunicationDirection Direction { get; set; } = CommunicationDirection.Outbound;

    public required string Subject { get; set; }

    /// <summary>Free-text summary / body / minutes.</summary>
    public string? Body { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Guid? PersonId { get; set; }

    public Person? Person { get; set; }

    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? WorkTaskId { get; set; }

    public WorkTask? WorkTask { get; set; }

    /// <summary>Counterpart label when it is not a stored person/company, e.g. "Soporte de Axon".</summary>
    public string? ContactLabel { get; set; }
}
