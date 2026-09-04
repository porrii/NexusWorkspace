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
