using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Bot;

public sealed class BotUpdate
{
    public BotUpdate(ChatMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public ChatMessage Message { get; }
}
