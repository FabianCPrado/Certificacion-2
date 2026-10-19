namespace Pedidos.Domain.Common;

// S4035 y S3875 apuntan a clases comunes; una entidad base de DDD está pensada para heredarse
// y comparar por identidad (Id + tipo concreto), así que la igualdad y los operadores son intencionales.
#pragma warning disable S4035, S3875

/// <summary>Entidad: se identifica por su Id, no por sus datos.</summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    // Requerido por EF Core
    protected Entity() => Id = default!;

    public TId Id { get; private set; }

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}

#pragma warning restore S4035, S3875
