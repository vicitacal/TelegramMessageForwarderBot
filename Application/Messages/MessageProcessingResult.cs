using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Messages;

public sealed class MessageProcessingResult
{
    public MessageProcessingResult(ChatMessage message, bool shouldBeForwarded)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ShouldBeForwarded = shouldBeForwarded;
    }

    public ChatMessage Message { get; }

    public bool ShouldBeForwarded { get; }
}

