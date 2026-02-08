using System.Net.Http.Json;
using System.Text.Json;
using NLog;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Infrastructure.Telegram;
using TelegramMessageForwarder.Application.Secrets;
using TelegramMessageForwarder.Domain.Messages;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using TL;
using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Bot;

public sealed class BotApiMessageSender : IMessageSender, IResponseSender
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string BotTokenSecretKey = "Telegram.BotToken";
    private const int SendMaxRetries = 3;
    private const int SendRetryDelayMs = 1000;

    private readonly IDestinationChatIdStore destinationStore;
    private readonly ISecretProvider secretProvider;
    private readonly ITelegramClientProvider telegramClientProvider;
    private readonly SemaphoreSlim botPeerLock = new(1, 1);
    private InputPeer? cachedBotPeer;

    public BotApiMessageSender(
        IDestinationChatIdStore destinationStore,
        ISecretProvider secretProvider,
        ITelegramClientProvider telegramClientProvider)
    {
        this.destinationStore = destinationStore ?? throw new ArgumentNullException(nameof(destinationStore));
        this.secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
        this.telegramClientProvider = telegramClientProvider ?? throw new ArgumentNullException(nameof(telegramClientProvider));
    }

    public async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Response text cannot be null or empty.", nameof(text));
        }

        var chatId = await destinationStore.GetAsync(cancellationToken);
        if (chatId == null)
        {
            Logger.Warn("Cannot send response: no destination chat registered. User must send /start to the bot first.");
            return;
        }

        await SendTextToChatAsync(chatId.Value, text, cancellationToken);
    }

    public Task SendToChatAsync(string text, long chatId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Response text cannot be null or empty.", nameof(text));
        }

        if (chatId == 0)
        {
            throw new ArgumentException("Chat identifier must be non-zero.", nameof(chatId));
        }

        return SendTextToChatAsync(chatId, text, cancellationToken);
    }

    public async Task SendAsync(ChatMessage message, object? forwardContext, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (forwardContext is (InputPeer fromPeer, int messageId))
        {
            await ForwardMessageViaMtProtoAsync(fromPeer, messageId, message, cancellationToken);
            return;
        }

        var chatId = await destinationStore.GetAsync(cancellationToken);
        if (chatId == null)
        {
            Logger.Warn("Cannot forward message {MessageId}: no destination chat registered.", message.MessageId.Value);
            return;
        }

        Logger.Debug("Forwarding message {MessageId} to bot chat (text fallback).", message.MessageId.Value);
        await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
    }

    private async Task ForwardMessageViaMtProtoAsync(InputPeer fromPeer, int messageId, ChatMessage message, CancellationToken cancellationToken)
    {
        var chatId = await destinationStore.GetAsync(cancellationToken);
        if (!chatId.HasValue)
        {
            Logger.Warn("Cannot forward message {MessageId}: no destination chat registered.", messageId);
            return;
        }

        var toPeer = await GetBotInputPeerAsync(cancellationToken);
        if (toPeer == null)
        {
            Logger.Warn("Cannot resolve bot peer for forwarding; falling back to text send.");
            await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
            return;
        }

        var client = await telegramClientProvider.CreateClientAsync(cancellationToken);
        var randomId = DateTime.UtcNow.Ticks;

        try
        {
            await client.Messages_ForwardMessages(fromPeer, new[] { messageId }, new[] { randomId }, toPeer);
            Logger.Debug("Forwarded message {MessageId} via MTProto.", messageId);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "MTProto forward failed for message {MessageId}; falling back to text.", messageId);
            await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
        }
    }

    private async Task<InputPeer?> GetBotInputPeerAsync(CancellationToken cancellationToken)
    {
        if (cachedBotPeer != null)
        {
            return cachedBotPeer;
        }

        await botPeerLock.WaitAsync(cancellationToken);
        try
        {
            if (cachedBotPeer != null)
            {
                return cachedBotPeer;
            }

            var botToken = secretProvider.GetSecret(BotTokenSecretKey);
            if (string.IsNullOrEmpty(botToken))
            {
                return null;
            }

            var botUsername = await GetBotUsernameFromApiAsync(botToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(botUsername))
            {
                Logger.Warn("Bot has no username; cannot resolve peer for MTProto forward.");
                return null;
            }

            var client = await telegramClientProvider.CreateClientAsync(cancellationToken);
            var resolved = await client.Contacts_ResolveUsername(botUsername.Trim().TrimStart('@'));
            if (resolved?.peer is PeerUser peerUser)
            {
                var user = resolved.users?.FirstOrDefault(u => u.Value.id == peerUser.user_id);
                if (user?.Value != null)
                {
                    cachedBotPeer = new InputPeerUser(user.Value.Value.id, user.Value.Value.access_hash);
                    return cachedBotPeer;
                }
            }

            Logger.Warn("ResolveUsername did not return a user peer for bot.");
            return null;
        }
        finally
        {
            botPeerLock.Release();
        }
    }

    private static async Task<string?> GetBotUsernameFromApiAsync(string botToken, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetFromJsonAsync<JsonElement>(
                $"https://api.telegram.org/bot{botToken}/getMe",
                cancellationToken);
            if (response.TryGetProperty("ok", out var ok) && ok.GetBoolean()
                && response.TryGetProperty("result", out var result)
                && result.TryGetProperty("username", out var username))
            {
                return username.GetString();
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private async Task SendTextToChatAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        var botToken = secretProvider.GetSecret(BotTokenSecretKey) ?? throw new Exception("Bot token environment variable is required.");
        var client = new TelegramBotClient(botToken, cancellationToken: cancellationToken);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= SendMaxRetries; attempt++)
        {
            try
            {
                var request = new SendMessageRequest() {
                    ChatId = new ChatId(chatId),
                    Text = text
                };
                await client.SendRequest(request, cancellationToken);
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
