using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

internal class DefaultForwardDecisionCommand : ICommandHandler
{

    private const string forwardWord = "forward";
    private const string discardWord = "discard";
    private const string getWord = "get";
    private const string setWord = "set";
    private const string UsageMessage = $"Usage: /defaultForward {setWord} <{forwardWord}|{discardWord}> <chat_id> | /defaultForward {getWord} <chat_id>";
    
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
        var action = command.Arguments[0];
        bool? actionBool = action switch
        {
            getWord => true,
            setWord => false,
            _ => null
        };
        if (!actionBool.HasValue)
        {
            await _responseSender.SendAsync($"Unable to parse action: {action}. {UsageMessage}", cancellationToken);
            return;
        }

        var chatIdString = command.Arguments[actionBool.Value ? 1 : 2];
        if (!int.TryParse(chatIdString, out var chatId))
        {
            await _responseSender.SendAsync($"Unable to parse chat id: {chatIdString}. {UsageMessage}", cancellationToken);
            return;
        }

        if (actionBool.Value)
        {
            var configuration = await _configurationStore.GetConfigurationAsync(cancellationToken);
            var chatConfig = configuration.GetChatConfigurations().FirstOrDefault(c => c.SourceChatId.Value == chatId);
            if (chatConfig == null)
            {
                await _responseSender.SendAsync($"Source chat {chatId} is not configured. Add it with /sources add first.", cancellationToken);
                return;
            }
            await _responseSender.SendAsync($"Default forward decision for chat {chatId} is {(chatConfig.DefaultForwardDesigion ? forwardWord : discardWord)}", cancellationToken);
            return;
        } else
        {
            var argument = command.Arguments[1].Trim().ToLowerInvariant();
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
            await SetDefaultForward(chatId, newForward.Value, cancellationToken);
        }

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
