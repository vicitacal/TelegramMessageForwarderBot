using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class AddSourceCommandHandler : ICommandHandler
{
    private const string AddSourceCommandName = "addsource";

    private readonly IResponseSender responseSender;
    private readonly IForwardingConfigurationStore configurationStore;

    public AddSourceCommandHandler(IResponseSender responseSender, IForwardingConfigurationStore configurationStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
    }

    public string CommandName => AddSourceCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        if (command.Arguments.Count == 0)
        {
            await responseSender.SendAsync("Usage: /addsource <chat_id>", cancellationToken);
            return;
        }

        if (!long.TryParse(command.Arguments[0], out var chatIdValue) || chatIdValue == 0)
        {
            await responseSender.SendAsync("Invalid chat ID. Usage: /addsource <chat_id>", cancellationToken);
            return;
        }

        var sourceChatId = new ChatId(chatIdValue);
        await configurationStore.AddOrUpdateSourceChatAsync(sourceChatId, Array.Empty<string>(), Array.Empty<string>(), false, cancellationToken);
        await responseSender.SendAsync($"Added source chat {chatIdValue}. Messages from this chat will be forwarded.", cancellationToken);
    }
}
