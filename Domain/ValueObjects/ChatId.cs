namespace TelegramMessageForwarder.Domain.ValueObjects;

public readonly struct ChatId : IEquatable<ChatId>
{
    public ChatId(long value)
    {
        if (value == 0)
        {
            throw new ArgumentException("Chat identifier must be non-zero.", nameof(value));
        }

        Value = value;
    }

    public long Value { get; }

    public bool Equals(ChatId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ChatId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ChatId left, ChatId right) => left.Equals(right);

    public static bool operator !=(ChatId left, ChatId right) => !left.Equals(right);
}
