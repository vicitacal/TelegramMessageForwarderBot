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

    public async Task StartAsync(Func<ChatMessage, object?, CancellationToken, Task> messageHandler, CancellationToken cancellationToken)
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
                var client = await clientProvider.CreateClientAsync(cancellationToken);

                delayMs = retryOptions.InitialDelayMs;

                Logger.Info("Starting Telegram message source.");

                var channel = System.Threading.Channels.Channel.CreateUnbounded<(ChatMessage Message, object? ForwardContext)>();

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

                        var forwardContext = TryBuildForwardContext(updatesBase, message);
                        await channel.Writer.WriteAsync((chatMessage, forwardContext), cancellationToken);
                    }
                };

                await foreach (var (msg, forwardContext) in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await messageHandler(msg, forwardContext, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Telegram message source connection lost. Reconnecting in {DelayMs} ms.", delayMs);
                await clientProvider.InvalidateClientAsync(cancellationToken);
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

    private static object? TryBuildForwardContext(UpdatesBase updatesBase, Message message)
    {
        var inputPeer = TryGetInputPeer(updatesBase, message.Peer);
        if (inputPeer == null)
        {
            return null;
        }

        return (inputPeer, message.ID);
    }

    private static InputPeer? TryGetInputPeer(UpdatesBase updatesBase, Peer peer)
    {
        switch (peer)
        {
            case PeerChannel peerChannel:
                if (updatesBase.Chats?.FirstOrDefault(c => c.Value is Channel ch && ch.id == peerChannel.channel_id).Value is not Channel channel) {
                    return null;
                }
                return new InputPeerChannel(channel.id, channel.access_hash);
            case PeerChat peerChat:
                return new InputPeerChat(peerChat.chat_id);
            case PeerUser peerUser:
                var user = updatesBase.Users?.FirstOrDefault(u => u.Value.id == peerUser.user_id);
                if (user?.Value == null)
                {
                    return null;
                }
                return new InputPeerUser(user.Value.Value.id, user.Value.Value.access_hash);
            default:
                return null;
        }
    }
}

