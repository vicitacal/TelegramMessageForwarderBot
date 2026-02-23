using System.Collections.Generic;

namespace TelegramMessageForwarder.Application.Messaging;

public sealed class BotKeyboard
{
    public required IReadOnlyList<IReadOnlyList<BotKeyboardButton>> Rows { get; init; }

    public bool ResizeKeyboard { get; init; } = true;

    public bool OneTimeKeyboard { get; init; }
}

public sealed class BotKeyboardButton
{
    public required string Text { get; init; }
}

