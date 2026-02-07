using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class ListSourcesCommandHandler : ICommandHandler
{
    private const string ListCommandName = "list";

    private readonly IResponseSender responseSender;
    private readonly IForwardingConfigurationStore configurationStore;

    public ListSourcesCommandHandler(IResponseSender responseSender, IForwardingConfigurationStore configurationStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
    }

    public string CommandName => ListCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetConfigurationAsync(cancellationToken);
        var configs = configuration.GetChatConfigurations();

        if (configs.Count == 0)
        {
            await responseSender.SendAsync("No source chats configured. Use /addsource <chat_id> to add one.", cancellationToken);
            return;
        }

        var lines = configs.Select(c => $"• {c.SourceChatId.Value} (whitelist: {c.GetWhitelistWords().Count} words, blacklist: {c.GetBlacklistWords().Count} words)");
        var text = "Source chats:\n" + string.Join("\n", lines);
        await responseSender.SendAsync(text, cancellationToken);
    }
}
