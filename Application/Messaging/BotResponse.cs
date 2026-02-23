namespace TelegramMessageForwarder.Application.Messaging;

public sealed class BotResponse
{
    public required string Text { get; init; }

    public BotKeyboard? Keyboard { get; init; }
}

