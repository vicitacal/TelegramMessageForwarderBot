using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class SourcesCommandHandler : ICommandHandler
{
    private const string SourcesCommandName = "sources";
    private const string ListSubcommand = "list";
    private const string AddSubcommand = "add";
    private const string RemoveSubcommand = "remove";

    private static readonly string UsageMessage = $"Usage: /sources {ListSubcommand} | /sources {AddSubcommand} <chat_id> | /sources {RemoveSubcommand} <chat_id>";

    private readonly IResponseSender responseSender;
    private readonly IForwardingConfigurationStore configurationStore;

    public SourcesCommandHandler(IResponseSender responseSender, IForwardingConfigurationStore configurationStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
    }

    public string CommandName => SourcesCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        if (command.Arguments.Count == 0)
        {
            await responseSender.SendAsync(UsageMessage, cancellationToken);
            return;
        }

        var subcommand = command.Arguments[0].Trim().ToLowerInvariant();
        if (subcommand != ListSubcommand && subcommand != AddSubcommand && subcommand != RemoveSubcommand)
        {
            await responseSender.SendAsync(UsageMessage, cancellationToken);
            return;
        }

        if (subcommand == ListSubcommand)
        {
            await HandleListAsync(cancellationToken);
            return;
        }

        if (command.Arguments.Count < 2)
        {
            await responseSender.SendAsync($"Usage: /sources {subcommand} <chat_id>", cancellationToken);
            return;
        }

        if (!long.TryParse(command.Arguments[1], out var chatIdValue) || chatIdValue == 0)
        {
            await responseSender.SendAsync($"Invalid chat ID. Usage: /sources {subcommand} <chat_id>", cancellationToken);
            return;
        }

        if (subcommand == AddSubcommand)
        {
            await HandleAddAsync(chatIdValue, cancellationToken);
        }
        else
        {
            await HandleRemoveAsync(chatIdValue, cancellationToken);
        }
    }

    private async Task HandleListAsync(CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetConfigurationAsync(cancellationToken);
        var configs = configuration.GetChatConfigurations();

        if (configs.Count == 0)
        {
            await responseSender.SendAsync("No source chats configured. Use /sources add <chat_id> to add one.", cancellationToken);
            return;
        }

        var lines = configs.Select(c => $"• {c.SourceChatId.Value} (whitelist: {c.GetWhitelistWords().Count} words, blacklist: {c.GetBlacklistWords().Count} words)");
        var text = "Source chats:\n" + string.Join("\n", lines);
        await responseSender.SendAsync(text, cancellationToken);
    }

    private async Task HandleAddAsync(long chatIdValue, CancellationToken cancellationToken)
    {
        var sourceChatId = new ChatId(chatIdValue);
        await configurationStore.AddOrUpdateSourceChatAsync(sourceChatId, Array.Empty<string>(), Array.Empty<string>(), false, cancellationToken);
        await responseSender.SendAsync($"Added source chat {chatIdValue}. Messages from this chat will be forwarded.", cancellationToken);
    }

    private async Task HandleRemoveAsync(long chatIdValue, CancellationToken cancellationToken)
    {
        var sourceChatId = new ChatId(chatIdValue);
        await configurationStore.RemoveSourceChatAsync(sourceChatId, cancellationToken);
        await responseSender.SendAsync($"Removed source chat {chatIdValue}.", cancellationToken);
    }
}
