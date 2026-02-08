using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class BlacklistCommandHandler : ICommandHandler
{
    private const string BlacklistCommandName = "blacklist";
    private const string ListSubcommand = "list";
    private const string AddSubcommand = "add";
    private const string RemoveSubcommand = "remove";

    private static readonly string UsageMessage = $"Usage: /blacklist {ListSubcommand} <chat_id> | /blacklist {AddSubcommand} <chat_id> <word1> [word2 ...] | /blacklist {RemoveSubcommand} <chat_id> <word1> [word2 ...]";

    private readonly IResponseSender responseSender;
    private readonly IForwardingConfigurationStore configurationStore;

    public BlacklistCommandHandler(IResponseSender responseSender, IForwardingConfigurationStore configurationStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
    }

    public string CommandName => BlacklistCommandName;

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
                await responseSender.SendAsync($"Usage: /blacklist {ListSubcommand} <chat_id>", cancellationToken);
                return;
            }

            if (!long.TryParse(command.Arguments[1], out var listChatId) || listChatId == 0)
            {
                await responseSender.SendAsync($"Invalid chat ID. Usage: /blacklist {ListSubcommand} <chat_id>", cancellationToken);
                return;
            }

            await HandleListAsync(listChatId, cancellationToken);
            return;
        }

        if (command.Arguments.Count < 3)
        {
            await responseSender.SendAsync($"Usage: /blacklist {subcommand} <chat_id> <word1> [word2 ...]", cancellationToken);
            return;
        }

        if (!long.TryParse(command.Arguments[1], out var chatIdValue) || chatIdValue == 0)
        {
            await responseSender.SendAsync($"Invalid chat ID. Usage: /blacklist {subcommand} <chat_id> <word1> [word2 ...]", cancellationToken);
            return;
        }

        var words = command.Arguments.Skip(2).Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w.Trim()).ToList();
        if (words.Count == 0)
        {
            await responseSender.SendAsync($"No words provided. Usage: /blacklist {subcommand} <chat_id> <word1> [word2 ...]", cancellationToken);
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

        var words = chatConfig.GetBlacklistWords().ToList();
        if (words.Count == 0)
        {
            await responseSender.SendAsync($"Blacklist for chat {chatIdValue} is empty.", cancellationToken);
            return;
        }

        var text = $"Blacklist for chat {chatIdValue}:\n" + string.Join("\n", words);
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

        var existingBlacklist = chatConfig.GetBlacklistWords().ToList();
        foreach (var word in words)
        {
            if (!existingBlacklist.Contains(word, StringComparer.OrdinalIgnoreCase))
            {
                existingBlacklist.Add(word);
            }
        }

        await configurationStore.AddOrUpdateSourceChatAsync(
            chatConfig.SourceChatId,
            chatConfig.GetWhitelistWords().ToList(),
            existingBlacklist,
            chatConfig.IsCaseSensitive,
            cancellationToken);

        await responseSender.SendAsync($"Added {words.Count} word(s) to blacklist for chat {chatIdValue}.", cancellationToken);
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

        var existingBlacklist = chatConfig.GetBlacklistWords().ToList();
        var removed = 0;
        foreach (var word in words)
        {
            var toRemove = existingBlacklist.Where(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var w in toRemove)
            {
                existingBlacklist.Remove(w);
                removed++;
            }
        }

        await configurationStore.AddOrUpdateSourceChatAsync(
            chatConfig.SourceChatId,
            chatConfig.GetWhitelistWords().ToList(),
            existingBlacklist,
            chatConfig.IsCaseSensitive,
            cancellationToken);

        await responseSender.SendAsync($"Removed {removed} word(s) from blacklist for chat {chatIdValue}.", cancellationToken);
    }
}
