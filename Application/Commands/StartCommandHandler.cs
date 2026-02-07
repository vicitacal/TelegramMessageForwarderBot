using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class StartCommandHandler : ICommandHandler
{
    private const string StartCommandName = "start";
    private const string WelcomeMessage = "Welcome to Telegram Message Forwarder. Use /help for available commands.";

    private readonly IResponseSender responseSender;

    public StartCommandHandler(IResponseSender responseSender)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
    }

    public string CommandName => StartCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        await responseSender.SendAsync(WelcomeMessage, cancellationToken);
    }
}
