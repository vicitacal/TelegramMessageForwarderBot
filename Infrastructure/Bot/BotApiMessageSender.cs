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
using Telegram.Bot.Types.Enums;
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
    private readonly ISourcePeerResolver sourcePeerResolver;
    private readonly SemaphoreSlim botPeerLock = new(1, 1);
    private InputPeer? cachedBotPeer;

    public BotApiMessageSender(
        IDestinationChatIdStore destinationStore,
        ISecretProvider secretProvider,
        ITelegramClientProvider telegramClientProvider,
        ISourcePeerResolver sourcePeerResolver)
    {
        this.destinationStore = destinationStore ?? throw new ArgumentNullException(nameof(destinationStore));
        this.secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
        this.telegramClientProvider = telegramClientProvider ?? throw new ArgumentNullException(nameof(telegramClientProvider));
        this.sourcePeerResolver = sourcePeerResolver ?? throw new ArgumentNullException(nameof(sourcePeerResolver));
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

        if (forwardContext is (long sourceChatId, int messageId))
        {
            await ForwardMessageViaMtProtoAsync(sourceChatId, messageId, message, cancellationToken);
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

    private async Task ForwardMessageViaMtProtoAsync(long sourceChatId, int messageId, ChatMessage message, CancellationToken cancellationToken)
    {
        var chatId = await destinationStore.GetAsync(cancellationToken);
        if (!chatId.HasValue)
        {
            Logger.Warn("Cannot forward message {MessageId}: no destination chat registered.", messageId);
            return;
        }

        var client = await telegramClientProvider.CreateClientAsync(cancellationToken);
        var fromPeer = await sourcePeerResolver.ResolveAsync(sourceChatId, cancellationToken);
        if (fromPeer == null)
        {
            Logger.Warn("Cannot resolve source peer for chat {SourceChatId}; falling back to text.", sourceChatId);
            await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
            return;
        }

        var toPeer = await GetBotInputPeerAsync(cancellationToken);
        if (toPeer == null)
        {
            Logger.Warn("Cannot resolve bot peer for forwarding; falling back to text send.");
            await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
            return;
        }

        var chatInfo = await sourcePeerResolver.GetChatInfoAsync(sourceChatId, cancellationToken);
        var link = BuildMessageLink(sourceChatId, messageId, chatInfo);
        var infoMessage = BuildInfoMessage(chatInfo, link);

        try
        {
            await SendTextToChatAsync(chatId.Value, infoMessage, ParseMode.MarkdownV2, cancellationToken);
            
            var randomId = DateTime.UtcNow.Ticks;
            await client.Messages_ForwardMessages(fromPeer, new[] { messageId }, new[] { randomId }, toPeer);
            Logger.Debug("Forwarded message {MessageId} via MTProto with info message.", messageId);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "MTProto forward failed for message {MessageId}; falling back to text.", messageId);
            await SendTextToChatAsync(chatId.Value, message.Text.Value, cancellationToken);
        }
    }

    private static string BuildMessageLink(long sourceChatId, int messageId, SourceChatInfo? chatInfo)
    {
        if (chatInfo?.Username != null)
        {
            return $"https://t.me/{chatInfo.Username}/{messageId}";
        }

        long chatIdForLink = chatInfo?.ChatId ?? sourceChatId;
        if (chatIdForLink < 0)
        {
            var fullId = -chatIdForLink;
            if (fullId > 1000000000000)
            {
                chatIdForLink = fullId - 1000000000000;
            }
            else
            {
                chatIdForLink = fullId;
            }
        }

        return $"https://t.me/c/{chatIdForLink}/{messageId}";
    }

    private static string BuildInfoMessage(SourceChatInfo? chatInfo, string link)
    {
        var chatName = EscapeMarkdownV2(chatInfo?.Name ?? "Unknown");
        return $"📨 Forwarded from: {chatName}\n🔗 [Open original message]({link})";
    }

    private static string EscapeMarkdownV2(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("\\", "\\\\")
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
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
                var user = resolved.users?.FirstOrDefault(u => u.Value.id == peerUser.user_id).Value;
                if (user != null)
                {
                    cachedBotPeer = new InputPeerUser(user.id, user.access_hash);
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
        await SendTextToChatAsync(chatId, text, null, cancellationToken);
    }

    private async Task SendTextToChatAsync(long chatId, string text, ParseMode? parseMode, CancellationToken cancellationToken)
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
                    Text = text,
                    ParseMode = parseMode ?? ParseMode.None
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
