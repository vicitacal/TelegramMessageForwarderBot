using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    private const string HelpCommandName = "help";
    private const string HelpMessage = "Commands:\n/start - Register this chat and get welcome message\n/help - Show this help\n/addsource <chat_id> - Add a source chat to forward from\n/list - List configured source chats";

    private readonly IResponseSender responseSender;

    public HelpCommandHandler(IResponseSender responseSender)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
    }

    public string CommandName => HelpCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        await responseSender.SendAsync(HelpMessage, cancellationToken);
    }
}
