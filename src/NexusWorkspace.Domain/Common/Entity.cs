namespace NexusWorkspace.Domain.Common;

/// <summary>
/// Base type for every persisted entity. Identity is a time-ordered GUID (v7),
/// so primary keys sort chronologically and stay merge-friendly for a future
/// multi-device sync without a schema rewrite.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public override bool Equals(object? obj)
        => obj is Entity other
           && other.GetType() == GetType()
           && Id != Guid.Empty
           && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
