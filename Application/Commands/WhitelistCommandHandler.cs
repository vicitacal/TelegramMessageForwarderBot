using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class WhitelistCommandHandler : ICommandHandler
{
    private const string WhitelistCommandName = "whitelist";
    private const string ListSubcommand = "list";
    private const string AddSubcommand = "add";
    private const string RemoveSubcommand = "remove";

    private static readonly string UsageMessage = $"Usage: /whitelist {ListSubcommand} <chat_id> | /whitelist {AddSubcommand} <chat_id> <word1> [word2 ...] | /whitelist {RemoveSubcommand} <chat_id> <word1> [word2 ...]";

    private readonly IResponseSender responseSender;
    private readonly IForwardingConfigurationStore configurationStore;

    public WhitelistCommandHandler(IResponseSender responseSender, IForwardingConfigurationStore configurationStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
    }

    public string CommandName => WhitelistCommandName;

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
            if (command.Arguments.Count < 2)
            {
                await responseSender.SendAsync($"Usage: /whitelist {ListSubcommand} <chat_id>", cancellationToken);
                return;
            }

            if (!long.TryParse(command.Arguments[1], out var listChatId) || listChatId == 0)
            {
                await responseSender.SendAsync($"Invalid chat ID. Usage: /whitelist {ListSubcommand} <chat_id>", cancellationToken);
                return;
            }

            await HandleListAsync(listChatId, cancellationToken);
            return;
        }

        if (command.Arguments.Count < 3)
        {
            await responseSender.SendAsync($"Usage: /whitelist {subcommand} <chat_id> <word1> [word2 ...]", cancellationToken);
            return;
        }

        if (!long.TryParse(command.Arguments[1], out var chatIdValue) || chatIdValue == 0)
        {
            await responseSender.SendAsync($"Invalid chat ID. Usage: /whitelist {subcommand} <chat_id> <word1> [word2 ...]", cancellationToken);
            return;
        }

        var words = command.Arguments.Skip(2).Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w.Trim()).ToList();
        if (words.Count == 0)
        {
            await responseSender.SendAsync($"No words provided. Usage: /whitelist {subcommand} <chat_id> <word1> [word2 ...]", cancellationToken);
            return;
        }

        if (subcommand == AddSubcommand)
        {
            await HandleAddAsync(chatIdValue, words, cancellationToken);
        }
        else
        {
            await HandleRemoveAsync(chatIdValue, words, cancellationToken);
        }
    }

    private async Task HandleListAsync(long chatIdValue, CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetConfigurationAsync(cancellationToken);
        var chatConfig = configuration.GetChatConfigurations().FirstOrDefault(c => c.SourceChatId.Value == chatIdValue);
        if (chatConfig == null)
        {
            await responseSender.SendAsync($"Source chat {chatIdValue} is not configured. Add it with /sources add first.", cancellationToken);
            return;
        }

        var words = chatConfig.GetWhitelistWords().ToList();
        if (words.Count == 0)
        {
            await responseSender.SendAsync($"Whitelist for chat {chatIdValue} is empty.", cancellationToken);
            return;
        }

        var text = $"Whitelist for chat {chatIdValue}:\n" + string.Join("\n", words);
        await responseSender.SendAsync(text, cancellationToken);
    }

    private async Task HandleAddAsync(long chatIdValue, List<string> words, CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetConfigurationAsync(cancellationToken);
        var chatConfig = configuration.GetChatConfigurations().FirstOrDefault(c => c.SourceChatId.Value == chatIdValue);
        if (chatConfig == null)
        {
            await responseSender.SendAsync($"Source chat {chatIdValue} is not configured. Add it with /sources add first.", cancellationToken);
            return;
        }

        var existingWhitelist = chatConfig.GetWhitelistWords().ToList();
        foreach (var word in words)
        {
            if (!existingWhitelist.Contains(word, StringComparer.OrdinalIgnoreCase))
            {
                existingWhitelist.Add(word);
            }
        }

        await configurationStore.AddOrUpdateSourceChatAsync(
            chatConfig.SourceChatId,
            existingWhitelist,
            chatConfig.GetBlacklistWords().ToList(),
            chatConfig.IsCaseSensitive,
            cancellationToken);

        await responseSender.SendAsync($"Added {words.Count} word(s) to whitelist for chat {chatIdValue}.", cancellationToken);
    }

    private async Task HandleRemoveAsync(long chatIdValue, List<string> words, CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetConfigurationAsync(cancellationToken);
        var chatConfig = configuration.GetChatConfigurations().FirstOrDefault(c => c.SourceChatId.Value == chatIdValue);
        if (chatConfig == null)
        {
            await responseSender.SendAsync($"Source chat {chatIdValue} is not configured. Add it with /sources add first.", cancellationToken);
            return;
        }

        var existingWhitelist = chatConfig.GetWhitelistWords().ToList();
        var removed = 0;
        foreach (var word in words)
        {
            var toRemove = existingWhitelist.Where(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var w in toRemove)
            {
                existingWhitelist.Remove(w);
                removed++;
            }
        }

        await configurationStore.AddOrUpdateSourceChatAsync(
            chatConfig.SourceChatId,
            existingWhitelist,
            chatConfig.GetBlacklistWords().ToList(),
            chatConfig.IsCaseSensitive,
            cancellationToken);

        await responseSender.SendAsync($"Removed {removed} word(s) from whitelist for chat {chatIdValue}.", cancellationToken);
    }
}
