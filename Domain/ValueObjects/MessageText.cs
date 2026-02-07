namespace TelegramMessageForwarder.Domain.ValueObjects;

public readonly struct MessageText : IEquatable<MessageText>
{
    public MessageText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Message text cannot be null or whitespace.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(MessageText other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is MessageText other && Equals(other);

    public override int GetHashCode() => Value?.GetHashCode(StringComparison.Ordinal) ?? 0;

    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(MessageText left, MessageText right) => left.Equals(right);

    public static bool operator !=(MessageText left, MessageText right) => !left.Equals(right);
}
