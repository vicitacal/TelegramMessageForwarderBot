namespace TelegramMessageForwarder.Application.Messaging;

public sealed class DefaultBotKeyboardProvider : IBotKeyboardProvider
{
    private const string HelpButtonText = "/help";
    private const string ListChatsButtonText = "/listchats";
    private const string SourcesListButtonText = "/sources list";
    private const string UsersListButtonText = "/users list";

    public BotKeyboard GetMainMenuKeyboard()
    {
        return new BotKeyboard
        {
            Rows =
            [
                [
                    new BotKeyboardButton { Text = HelpButtonText },
                    new BotKeyboardButton { Text = ListChatsButtonText }
                ],
                [
                    new BotKeyboardButton { Text = SourcesListButtonText },
                    new BotKeyboardButton { Text = UsersListButtonText }
                ]
            ]
        };
    }
}

