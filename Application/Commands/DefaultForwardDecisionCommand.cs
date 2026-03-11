using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

internal class DefaultForwardDecisionCommand : ICommandHandler
{

    private const string forwardWord = "forward";
    private const string discardWord = "discard";
    private const string UsageMessage = $"Usage: /defaultForward <{forwardWord}|{discardWord}> <chat_id>";
    
    public string CommandName => "defaultForward";

    public DefaultForwardDecisionCommand(IResponseSender responseSender, IForwardingConfigurationStore configurationStore)
    {
        _responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
    }

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        if (command.Arguments.Count < 2)
        {
            await _responseSender.SendAsync(UsageMessage, cancellationToken);
            return;
        }
        var argument = command.Arguments[0].Trim().ToLowerInvariant();
        bool? newForward = argument switch
        {
            forwardWord => true,
            "true" => true,
            discardWord => false,
            "false" => false,
            _ => null
        };
        if (!newForward.HasValue) {
            await _responseSender.SendAsync($"Unable to parse argument: {argument}. {UsageMessage}", cancellationToken);
            return;
        }
        if (!int.TryParse(command.Arguments[1], out var chatId))
        {
            await _responseSender.SendAsync($"Unable to parse chat id: {command.Arguments[1]}. {UsageMessage}", cancellationToken);
            return;
        }
        await SetDefaultForward(chatId, newForward.Value, cancellationToken);
    }

    private async Task SetDefaultForward(long chatIdValue, bool defaultForwardDecision, CancellationToken cancellationToken)
    {
        var configuration = await _configurationStore.GetConfigurationAsync(cancellationToken);
        var chatConfig = configuration.GetChatConfigurations().FirstOrDefault(c => c.SourceChatId.Value == chatIdValue);
        if (chatConfig == null)
        {
            await _responseSender.SendAsync($"Source chat {chatIdValue} is not configured. Add it with /sources add first.", cancellationToken);
            return;
        }

        await _configurationStore.AddOrUpdateSourceChatAsync(
            chatConfig.SourceChatId,
            chatConfig.GetWhitelistWords().ToList(),
            chatConfig.GetBlacklistWords().ToList(),
            chatConfig.IsCaseSensitive,
            defaultForwardDecision,
            cancellationToken);

        await _responseSender.SendAsync($"Default decision is {(defaultForwardDecision ? "forward" : "discard")} for {chatIdValue}.", cancellationToken);
    }

    private readonly IResponseSender _responseSender;
    private readonly IForwardingConfigurationStore _configurationStore;

}
