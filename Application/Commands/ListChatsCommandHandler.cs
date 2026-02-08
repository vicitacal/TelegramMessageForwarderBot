using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Chats;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class ListChatsCommandHandler : ICommandHandler
{
    private const string ListChatsCommandName = "listchats";
    private const int TelegramMaxMessageLength = 4096;

    private readonly IResponseSender responseSender;
    private readonly IChatListProvider chatListProvider;

    public ListChatsCommandHandler(IResponseSender responseSender, IChatListProvider chatListProvider)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.chatListProvider = chatListProvider ?? throw new ArgumentNullException(nameof(chatListProvider));
    }

    public string CommandName => ListChatsCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        var chats = await chatListProvider.GetChatsAsync(cancellationToken);

        if (chats.Count == 0)
        {
            await responseSender.SendAsync("No chats found.", cancellationToken);
            return;
        }

        var lines = chats.Select(c => $"{c.ChatId}: {EscapeName(c.Name)}");
        var fullText = "Chat ID — Name:\n" + string.Join("\n", lines);

        if (fullText.Length <= TelegramMaxMessageLength)
        {
            await responseSender.SendAsync(fullText, cancellationToken);
            return;
        }

        var chunk = new System.Text.StringBuilder(TelegramMaxMessageLength - 50);
        var header = "Chat ID — Name:\n";
        chunk.Append(header);

        foreach (var line in lines)
        {
            var next = line + "\n";
            if (chunk.Length + next.Length > TelegramMaxMessageLength - 50)
            {
                await responseSender.SendAsync(chunk.ToString().TrimEnd(), cancellationToken);
                chunk.Clear();
                chunk.Append(next);
            }
            else
            {
                chunk.Append(next);
            }
        }

        if (chunk.Length > header.Length)
        {
            await responseSender.SendAsync(chunk.ToString().TrimEnd(), cancellationToken);
        }
    }

    private static string EscapeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "(no name)";
        }

        return name.Replace("\r", " ").Replace("\n", " ");
    }
}
