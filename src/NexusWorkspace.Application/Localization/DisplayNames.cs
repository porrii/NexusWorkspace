using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Localization;

/// <summary>
/// Spanish display names for domain enums. Single source of truth shared by
/// services (history summaries) and the UI (labels, chips).
/// </summary>
public static class DisplayNames
{
    public static string Of(ProjectStatus status) => status switch
    {
        ProjectStatus.Planning => "Planificación",
        ProjectStatus.Active => "Activo",
        ProjectStatus.OnHold => "En pausa",
        ProjectStatus.Blocked => "Bloqueado",
        ProjectStatus.Finished => "Finalizado",
        _ => status.ToString(),
    };

    public static string Of(WorkTaskStatus status) => status switch
    {
        WorkTaskStatus.Pending => "Pendiente",
        WorkTaskStatus.InProgress => "En progreso",
        WorkTaskStatus.WaitingClient => "Esperando cliente",
        WorkTaskStatus.WaitingProvider => "Esperando proveedor",
        WorkTaskStatus.Blocked => "Bloqueada",
        WorkTaskStatus.Finished => "Finalizada",
        WorkTaskStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };

    public static string Of(Priority priority) => priority switch
    {
        Priority.Critical => "Crítica",
        Priority.High => "Alta",
        Priority.Medium => "Media",
        Priority.Low => "Baja",
        _ => priority.ToString(),
    };

    public static string Of(CompanyKind kind) => kind switch
    {
        CompanyKind.Client => "Cliente",
        CompanyKind.Provider => "Proveedor",
        CompanyKind.Internal => "Interno",
        _ => "Otro",
    };

    public static string Of(FollowUpState state) => state switch
    {
        FollowUpState.Waiting => "Esperando",
        FollowUpState.Escalated => "Escalado",
        FollowUpState.Answered => "Respondido",
        FollowUpState.Closed => "Cerrado",
        _ => state.ToString(),
    };

    public static string Of(CommunicationChannel channel) => channel switch
    {
        CommunicationChannel.Email => "Correo",
        CommunicationChannel.Call => "Llamada",
        CommunicationChannel.Chat => "Chat",
        CommunicationChannel.InPerson => "En persona",
        CommunicationChannel.Letter => "Carta",
        CommunicationChannel.Ticket => "Ticket",
        _ => "Otro",
    };

    public static string Of(CommunicationDirection direction) => direction switch
    {
        CommunicationDirection.Outbound => "Enviado",
        CommunicationDirection.Inbound => "Recibido",
        CommunicationDirection.Internal => "Interno",
        _ => direction.ToString(),
    };

    public static string Of(MeetingStatus status) => status switch
    {
        MeetingStatus.Scheduled => "Programada",
        MeetingStatus.Held => "Realizada",
        MeetingStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };

    public static string Of(RelationKind kind) => kind switch
    {
        RelationKind.RelatesTo => "Relacionado con",
        RelationKind.Blocks => "Bloquea a",
        RelationKind.DependsOn => "Depende de",
        RelationKind.Duplicates => "Duplica a",
        RelationKind.References => "Hace referencia a",
        RelationKind.PartOf => "Forma parte de",
        RelationKind.Mentions => "Menciona a",
        _ => kind.ToString(),
    };

    public static string Of(EntityKind kind) => kind switch
    {
        EntityKind.Project => "Proyecto",
        EntityKind.WorkTask => "Tarea",
        EntityKind.SubTask => "Subtarea",
        EntityKind.ChecklistItem => "Ítem de checklist",
        EntityKind.Person => "Persona",
        EntityKind.Company => "Empresa",
        EntityKind.Tag => "Etiqueta",
        EntityKind.Comment => "Comentario",
        EntityKind.Attachment => "Adjunto",
        EntityKind.FollowUp => "Seguimiento",
        EntityKind.Communication => "Comunicación",
        EntityKind.Meeting => "Reunión",
        EntityKind.Reminder => "Recordatorio",
        EntityKind.InboxItem => "Elemento del inbox",
        EntityKind.SavedSearch => "Búsqueda guardada",
        EntityKind.Relation => "Relación",
        _ => kind.ToString(),
    };

    public static string Of(ReminderStatus status) => status switch
    {
        ReminderStatus.Pending => "Pendiente",
        ReminderStatus.Done => "Hecho",
        ReminderStatus.Dismissed => "Descartado",
        _ => status.ToString(),
    };

    public static string Of(QuickActionKind action) => action switch
    {
        QuickActionKind.EmailSent => "Correo enviado",
        QuickActionKind.EmailReceived => "Correo recibido",
        QuickActionKind.CallMade => "Llamada realizada",
        QuickActionKind.MeetingHeld => "Reunión realizada",
        QuickActionKind.InfoSent => "Información enviada",
        QuickActionKind.InfoReceived => "Información recibida",
        QuickActionKind.PendingClient => "Pendiente cliente",
        QuickActionKind.PendingProvider => "Pendiente proveedor",
        QuickActionKind.IncidentDetected => "Incidencia detectada",
        QuickActionKind.IncidentResolved => "Incidencia solucionada",
        QuickActionKind.DeployedDev => "Desplegado DESA",
        QuickActionKind.DeployedPre => "Desplegado PRE",
        QuickActionKind.DeployedPro => "Desplegado PRO",
        QuickActionKind.ReminderSent => "Recordatorio enviado",
        QuickActionKind.ChangeRequested => "Cambio solicitado",
        QuickActionKind.TestPerformed => "Prueba realizada",
        _ => action.ToString(),
    };
}
