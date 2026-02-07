using System.Threading.Channels;
using NLog;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;
using TelegramMessageForwarder.Domain.ValueObjects;
using TL;
using DomainMessageText = TelegramMessageForwarder.Domain.ValueObjects.MessageText;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class TelegramMessageSource : IMessageSource, IAsyncDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ITelegramClientProvider clientProvider;

    public TelegramMessageSource(ITelegramClientProvider clientProvider)
    {
        this.clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    public async Task StartAsync(Func<ChatMessage, CancellationToken, Task> messageHandler, CancellationToken cancellationToken)
    {
        if (messageHandler == null)
        {
            throw new ArgumentNullException(nameof(messageHandler));
        }

        var retryOptions = new ConnectionRetryOptions();
        var delayMs = retryOptions.InitialDelayMs;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = await clientProvider.CreateClientAsync(cancellationToken);

                delayMs = retryOptions.InitialDelayMs;

                Logger.Info("Starting Telegram message source.");

                var channel = System.Threading.Channels.Channel.CreateUnbounded<ChatMessage>();

                client.OnUpdates += async (update) =>
                {
                    if (update is not UpdatesBase updatesBase)
                    {
                        return;
                    }

                    foreach (var updateItem in updatesBase.UpdateList)
                    {
                        if (updateItem is not UpdateNewMessage updateNewMessage)
                        {
                            continue;
                        }

                        if (updateNewMessage.message is not Message message)
                        {
                            continue;
                        }

                        var chatMessage = TryMapToDomainMessage(message);
                        if (chatMessage == null)
                        {
                            continue;
                        }

                        await channel.Writer.WriteAsync(chatMessage, cancellationToken);
                    }
                };

                await foreach (var chatMessage in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await messageHandler(chatMessage, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Telegram message source connection lost. Reconnecting in {DelayMs} ms.", delayMs);
                await Task.Delay(Math.Min(delayMs, retryOptions.MaxDelayMs), cancellationToken);
                delayMs = (int)(delayMs * retryOptions.BackoffMultiplier);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private static ChatMessage? TryMapToDomainMessage(Message tlMessage)
    {
        var rawText = tlMessage.message;
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var senderIdValue = tlMessage.from_id?.ID ?? 1;
        if (senderIdValue == 0)
        {
            senderIdValue = 1;
        }

        var messageId = new MessageId(tlMessage.ID);
        var chatId = new ChatId(tlMessage.Peer.ID);
        var senderId = new UserId(senderIdValue);
        var text = new DomainMessageText(rawText);
        var occurredAtUtc = DateTime.SpecifyKind(tlMessage.date, DateTimeKind.Utc);
        var isOutgoing = false; //What is the correct way to determine this?

        return new ChatMessage(messageId, chatId, senderId, text, occurredAtUtc, isOutgoing);
    }
}

