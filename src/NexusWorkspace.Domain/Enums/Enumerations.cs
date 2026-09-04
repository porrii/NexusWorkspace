namespace NexusWorkspace.Domain.Enums;

/// <summary>Lifecycle status of a project. Archival is a separate orthogonal flag.</summary>
public enum ProjectStatus
{
    Planning = 0,
    Active = 1,
    OnHold = 2,
    Blocked = 3,
    Finished = 4,
}

/// <summary>Lifecycle status of a task. "Archivada" is a separate orthogonal flag.</summary>
public enum WorkTaskStatus
{
    Pending = 0,
    InProgress = 1,
    WaitingClient = 2,
    WaitingProvider = 3,
    Blocked = 4,
    Finished = 5,
    Cancelled = 6,
}

/// <summary>Semantic priority, mapped to the colour system in the UI.</summary>
public enum Priority
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3,
}

/// <summary>
/// Kind of every entry in the immutable activity log. New members are appended;
/// existing numeric values never change.
/// </summary>
public enum ActivityType
{
    Created = 0,
    Updated = 1,
    StatusChanged = 2,
    PriorityChanged = 3,
    DueDateChanged = 4,
    AssigneeChanged = 5,
    CommentAdded = 6,
    AttachmentAdded = 7,
    AttachmentRemoved = 8,
    SubTaskAdded = 9,
    SubTaskCompleted = 10,
    ChecklistItemToggled = 11,
    DependencyAdded = 12,
    DependencyRemoved = 13,
    FollowUpStarted = 14,
    FollowUpReminderSent = 15,
    FollowUpResolved = 16,
    QuickAction = 17,
    RelationAdded = 18,
    RelationRemoved = 19,
    MovedProject = 20,
    Archived = 21,
    Restored = 22,
    Trashed = 23,
    RestoredFromTrash = 24,
    Deleted = 25,
    Duplicated = 26,
    TemplateApplied = 27,
    Imported = 28,
    Renamed = 29,
}

/// <summary>One-click actions available from any project or task.</summary>
public enum QuickActionKind
{
    EmailSent = 0,
    EmailReceived = 1,
    CallMade = 2,
    MeetingHeld = 3,
    InfoSent = 4,
    InfoReceived = 5,
    PendingClient = 6,
    PendingProvider = 7,
    IncidentDetected = 8,
    IncidentResolved = 9,
    DeployedDev = 10,
    DeployedPre = 11,
    DeployedPro = 12,
    ReminderSent = 13,
    ChangeRequested = 14,
    TestPerformed = 15,
}

/// <summary>
/// Discriminator for polymorphic references (comments, attachments, activity,
/// follow-ups, reminders, communications) that can point at any entity.
/// </summary>
public enum EntityKind
{
    Project = 0,
    WorkTask = 1,
    SubTask = 2,
    ChecklistItem = 3,
    Person = 4,
    Company = 5,
    Tag = 6,
    Comment = 7,
    Attachment = 8,
    FollowUp = 9,
    Communication = 10,
    Meeting = 11,
    Reminder = 12,
    InboxItem = 13,
}

public enum CompanyKind
{
    Other = 0,
    Client = 1,
    Provider = 2,
    Internal = 3,
}

public enum DependencyKind
{
    FinishToStart = 0,
    Blocks = 1,
    Related = 2,
}

/// <summary>Lifecycle of a "waiting for a reply from…" follow-up.</summary>
public enum FollowUpState
{
    /// <summary>Still waiting.</summary>
    Waiting = 0,

    /// <summary>Still waiting, but flagged as overdue / chased hard.</summary>
    Escalated = 1,

    /// <summary>The other party replied.</summary>
    Answered = 2,

    /// <summary>Closed without needing the reply any more.</summary>
    Closed = 3,
}

public enum ReminderStatus
{
    Pending = 0,
    Done = 1,
    Dismissed = 2,
}

/// <summary>Kind of a local notification-centre entry.</summary>
public enum NotificationKind
{
    System = 0,
    Reminder = 1,
    FollowUp = 2,
    DueDate = 3,
}
