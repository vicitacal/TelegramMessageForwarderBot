namespace TelegramMessageForwarder.Domain.ValueObjects;

public readonly struct MessageId : IEquatable<MessageId>
{
    public MessageId(long value)
    {
        if (value == 0)
        {
            throw new ArgumentException("Message identifier must be non-zero.", nameof(value));
        }

        Value = value;
    }

    public long Value { get; }

    public bool Equals(MessageId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is MessageId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);

    public static bool operator !=(MessageId left, MessageId right) => !left.Equals(right);
}
