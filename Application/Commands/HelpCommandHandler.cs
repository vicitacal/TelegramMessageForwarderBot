using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    private const string HelpCommandName = "help";
    private const string HelpMessage = "Commands:\n/start - Register this chat for receiving forwarded messages\n/help - Show this help\n/users list|add|remove [user_id] - Allowed users\n/listchats - List all chat IDs with names (from Telegram)\n/sources list|add|remove [chat_id] - Source chats to forward from\n/whitelist list|add|remove <chat_id> [words...] - Whitelist words (always forward)\n/blacklist list|add|remove <chat_id> [words...] - Blacklist words (never forward)";

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
