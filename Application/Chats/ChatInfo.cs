namespace TelegramMessageForwarder.Application.Chats;

public sealed class ChatInfo
{
    public long ChatId { get; init; }

    public string Name { get; init; } = string.Empty;
}
