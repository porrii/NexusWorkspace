using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.QuickActions;

/// <summary>One entry in the quick-action bar shown on tasks and projects.</summary>
/// <param name="Kind">The action.</param>
/// <param name="Label">Spanish label for the button.</param>
/// <param name="Icon">Material icon key.</param>
/// <param name="PushesStatus">Status the target should move to, if any.</param>
public sealed record QuickActionDescriptor(
    QuickActionKind Kind,
    string Label,
    string Icon,
    WorkTaskStatus? PushesStatus);

/// <summary>The fixed set of quick actions and their presentation metadata.</summary>
public static class QuickActionCatalog
{
    public static IReadOnlyList<QuickActionDescriptor> All { get; } =
    [
        new(QuickActionKind.EmailSent, DisplayNames.Of(QuickActionKind.EmailSent), "EmailArrowRight", null),
        new(QuickActionKind.EmailReceived, DisplayNames.Of(QuickActionKind.EmailReceived), "EmailArrowLeft", null),
        new(QuickActionKind.CallMade, DisplayNames.Of(QuickActionKind.CallMade), "PhoneOutgoing", null),
        new(QuickActionKind.MeetingHeld, DisplayNames.Of(QuickActionKind.MeetingHeld), "AccountGroup", null),
        new(QuickActionKind.InfoSent, DisplayNames.Of(QuickActionKind.InfoSent), "Upload", null),
        new(QuickActionKind.InfoReceived, DisplayNames.Of(QuickActionKind.InfoReceived), "Download", null),
        new(QuickActionKind.PendingClient, DisplayNames.Of(QuickActionKind.PendingClient), "AccountClock", WorkTaskStatus.WaitingClient),
        new(QuickActionKind.PendingProvider, DisplayNames.Of(QuickActionKind.PendingProvider), "TruckDelivery", WorkTaskStatus.WaitingProvider),
        new(QuickActionKind.IncidentDetected, DisplayNames.Of(QuickActionKind.IncidentDetected), "AlertCircle", null),
        new(QuickActionKind.IncidentResolved, DisplayNames.Of(QuickActionKind.IncidentResolved), "CheckCircle", null),
        new(QuickActionKind.DeployedDev, DisplayNames.Of(QuickActionKind.DeployedDev), "Rocket", null),
        new(QuickActionKind.DeployedPre, DisplayNames.Of(QuickActionKind.DeployedPre), "Rocket", null),
        new(QuickActionKind.DeployedPro, DisplayNames.Of(QuickActionKind.DeployedPro), "RocketLaunch", null),
        new(QuickActionKind.ReminderSent, DisplayNames.Of(QuickActionKind.ReminderSent), "BellRing", null),
        new(QuickActionKind.ChangeRequested, DisplayNames.Of(QuickActionKind.ChangeRequested), "FileDocumentEdit", null),
        new(QuickActionKind.TestPerformed, DisplayNames.Of(QuickActionKind.TestPerformed), "TestTube", null),
    ];

    public static QuickActionDescriptor Get(QuickActionKind kind)
        => All.First(a => a.Kind == kind);
}
