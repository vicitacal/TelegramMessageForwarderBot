using NLog;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Secrets;
using TelegramMessageForwarder.Domain.Messages;
using TelegramMessageForwarder.Domain.ValueObjects;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramMessageForwarder.Infrastructure.Bot;

public sealed class TelegramBotUpdateReceiver : IBotUpdateReceiver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string BotTokenSecretKey = "Telegram.BotToken";

    private readonly ISecretProvider secretProvider;

    public TelegramBotUpdateReceiver(ISecretProvider secretProvider)
    {
        this.secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
    }

    public async Task StartAsync(Func<BotUpdate, CancellationToken, Task> updateHandler, CancellationToken cancellationToken)
    {
        var botToken = secretProvider.GetSecret(BotTokenSecretKey);
        var client = new TelegramBotClient(botToken, cancellationToken: cancellationToken);

        Logger.Info("Starting Bot API update receiver.");

        client.OnUpdate += async (update) =>
        {
            if (update.Message is not { } message)
            {
                return;
            }

            var chatMessage = MapToChatMessage(message);
            var botUpdate = new BotUpdate(chatMessage);
            await updateHandler(botUpdate, cancellationToken);
        };

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private static ChatMessage MapToChatMessage(Message message)
    {
        var messageId = new MessageId(message.Id);
        var chatId = new ChatId(message.Chat.Id);
        var senderIdValue = message.From?.Id ?? 1;
        if (senderIdValue == 0)
        {
            senderIdValue = 1;
        }
        var senderId = new UserId(senderIdValue);
        var rawText = message.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawText))
        {
            rawText = " ";
        }
        var text = new MessageText(rawText);
        var occurredAtUtc = message.Date.ToUniversalTime();
        var isOutgoing = false;

        return new ChatMessage(messageId, chatId, senderId, text, occurredAtUtc, isOutgoing);
    }
}
