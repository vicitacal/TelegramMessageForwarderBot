namespace TelegramMessageForwarder.Domain.ValueObjects;

public readonly struct UserId : IEquatable<UserId>
{
    public UserId(long value)
    {
        if (value == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(value));
        }

        Value = value;
    }

    public long Value { get; }

    public bool Equals(UserId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is UserId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(UserId left, UserId right) => left.Equals(right);

    public static bool operator !=(UserId left, UserId right) => !left.Equals(right);
}
