using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class StartCommandHandler : ICommandHandler
{
    private const string StartCommandName = "start";
    private const string WelcomeMessage = "Welcome to Telegram Message Forwarder. Forwarded messages will appear here. Use /help for commands.";

    private readonly IResponseSender responseSender;
    private readonly IBotKeyboardProvider keyboardProvider;
    private readonly IDestinationChatIdStore destinationStore;

    public StartCommandHandler(IResponseSender responseSender, IBotKeyboardProvider keyboardProvider, IDestinationChatIdStore destinationStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.keyboardProvider = keyboardProvider ?? throw new ArgumentNullException(nameof(keyboardProvider));
        this.destinationStore = destinationStore ?? throw new ArgumentNullException(nameof(destinationStore));
    }

    public string CommandName => StartCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        await destinationStore.SetAsync(message.ChatId.Value, cancellationToken);
        await responseSender.SendAsync(
            new BotResponse
            {
                Text = WelcomeMessage,
                Keyboard = keyboardProvider.GetMainMenuKeyboard()
            },
            cancellationToken);
    }
}
