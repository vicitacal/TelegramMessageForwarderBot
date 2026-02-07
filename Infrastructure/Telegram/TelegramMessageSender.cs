using NLog;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;
using TL;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class TelegramMessageSender : IMessageSender, IResponseSender
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const int SendMaxRetries = 3;
    private const int SendRetryDelayMs = 1000;

    private readonly ITelegramClientProvider clientProvider;

    public TelegramMessageSender(ITelegramClientProvider clientProvider)
    {
        this.clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    public async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Response text cannot be null or empty.", nameof(text));
        }

        using var client = await clientProvider.CreateClientAsync(cancellationToken);

        Logger.Debug("Sending response to bot chat.");

        Exception? lastException = null;

        for (var attempt = 1; attempt <= SendMaxRetries; attempt++)
        {
            try
            {
                await client.SendMessageAsync(InputPeer.Self, text);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.Warn(ex, "Response send attempt {Attempt}/{MaxRetries} failed.", attempt, SendMaxRetries);

                if (attempt < SendMaxRetries)
                {
                    await Task.Delay(SendRetryDelayMs, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException($"Failed to send response after {SendMaxRetries} attempts.", lastException);
    }

    public async Task SendAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        using var client = await clientProvider.CreateClientAsync(cancellationToken);

        Logger.Debug("Sending message {MessageId} to bot chat.", message.MessageId.Value);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= SendMaxRetries; attempt++)
        {
            try
            {
                await client.SendMessageAsync(InputPeer.Self, message.Text.Value);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.Warn(ex, "Send attempt {Attempt}/{MaxRetries} failed for message {MessageId}.", attempt, SendMaxRetries, message.MessageId.Value);

                if (attempt < SendMaxRetries)
                {
                    await Task.Delay(SendRetryDelayMs, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException($"Failed to send message {message.MessageId.Value} after {SendMaxRetries} attempts.", lastException);
    }
}

