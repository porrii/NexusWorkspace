namespace NexusWorkspace.Domain.Common;

/// <summary>Raised when a domain invariant or business rule is violated.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>Raised when a state machine rejects a requested status transition.</summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string entityName, object from, object to)
        : base($"Transición de estado no permitida en {entityName}: {from} → {to}.")
    {
        EntityName = entityName;
        From = from;
        To = to;
    }

    public string EntityName { get; }

    public object From { get; }

    public object To { get; }
}
