using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Domain.Messages;

public sealed class ChatMessage
{
    public ChatMessage(
        MessageId messageId,
        ChatId chatId,
        UserId senderId,
        MessageText text,
        DateTimeOffset occurredAtUtc,
        bool isOutgoing)
    {
        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be provided in UTC.", nameof(occurredAtUtc));
        }

        if (messageId.Value == 0)
        {
            throw new ArgumentException("Message identifier must be valid.", nameof(messageId));
        }

        if (chatId.Value == 0)
        {
            throw new ArgumentException("Chat identifier must be valid.", nameof(chatId));
        }

        if (senderId.Value == 0)
        {
            throw new ArgumentException("Sender identifier must be valid.", nameof(senderId));
        }

        if (string.IsNullOrWhiteSpace(text.Value))
        {
            throw new ArgumentException("Message text must be valid.", nameof(text));
        }

        MessageId = messageId;
        ChatId = chatId;
        SenderId = senderId;
        Text = text;
        OccurredAtUtc = occurredAtUtc;
        IsOutgoing = isOutgoing;
    }

    public MessageId MessageId { get; }

    public ChatId ChatId { get; }

    public UserId SenderId { get; }

    public MessageText Text { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public bool IsOutgoing { get; }
}

