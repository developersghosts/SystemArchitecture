namespace ServicesA.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null && GetType() == other.GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(0, (hash, c) => HashCode.Combine(hash, c));

    public static bool operator ==(ValueObject? a, ValueObject? b) => a is null ? b is null : a.Equals(b);

    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);
}
