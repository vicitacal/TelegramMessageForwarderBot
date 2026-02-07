using NLog;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Application.Secrets;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Infrastructure.Bot;

public sealed class BotApiMessageSender : IMessageSender, IResponseSender
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string BotTokenSecretKey = "Telegram.BotToken";
    private const int SendMaxRetries = 3;
    private const int SendRetryDelayMs = 1000;

    private readonly IDestinationChatIdStore destinationStore;
    private readonly ISecretProvider secretProvider;

    public BotApiMessageSender(IDestinationChatIdStore destinationStore, ISecretProvider secretProvider)
    {
        this.destinationStore = destinationStore ?? throw new ArgumentNullException(nameof(destinationStore));
        this.secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
    }

    public async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Response text cannot be null or empty.", nameof(text));
        }

        var chatId = destinationStore.Get();
        if (chatId == null)
        {
            Logger.Warn("Cannot send response: no destination chat registered. User must send /start to the bot first.");
            return;
        }

        await SendTextToChatAsync(chatId.Value, text, cancellationToken);
    }

    public async Task SendAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var chatId = destinationStore.Get();
        if (chatId == null)
        {
            Logger.Warn("Cannot forward message {MessageId}: no destination chat registered.", message.MessageId.Value);
            return;
        }

        Logger.Debug("Forwarding message {MessageId} to bot chat.", message.MessageId.Value);
        await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
    }

    private async Task SendTextToChatAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        var botToken = secretProvider.GetSecret(BotTokenSecretKey);
        var client = new Telegram.Bot.TelegramBotClient(botToken);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= SendMaxRetries; attempt++)
        {
            try
            {
                await client.SendMessageAsync(chatId: chatId, text: text, cancellationToken: cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.Warn(ex, "Bot API send attempt {Attempt}/{MaxRetries} failed.", attempt, SendMaxRetries);

                if (attempt < SendMaxRetries)
                {
                    await Task.Delay(SendRetryDelayMs, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException("Failed to send message via Bot API after all retries.", lastException);
    }
}
