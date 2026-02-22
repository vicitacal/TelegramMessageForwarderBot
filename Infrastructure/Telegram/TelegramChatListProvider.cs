using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Chats;
using TL;
using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class TelegramChatListProvider : IChatListProvider, ISourcePeerResolver
{
    private const long ChannelIdOffset = 1000000000000L;

    private readonly ITelegramClientProvider clientProvider;
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private Dictionary<long, ChatBase>? cachedChatsById;
    private Dictionary<long, User>? cachedUsersById;

    public TelegramChatListProvider(ITelegramClientProvider clientProvider)
    {
        this.clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    public async Task<IReadOnlyList<ChatInfo>> GetChatsAsync(CancellationToken cancellationToken)
    {
        var (chats, users) = await GetOrRefreshCacheAsync(cancellationToken);
        var result = new List<ChatInfo>(chats.Count + users.Count);

        foreach (var kv in chats)
        {
            if (kv.Value is Channel channel)
            {
                if (-ChannelIdOffset - channel.id != kv.Key)
                {
                    continue;
                }
            }
            var name = kv.Value switch
            {
                Channel channelValue => channelValue.title ?? channelValue.username ?? kv.Key.ToString(),
                Chat chat => chat.title ?? kv.Key.ToString(),
                _ => kv.Key.ToString()
            };
            result.Add(new ChatInfo { ChatId = kv.Key, Name = name ?? kv.Key.ToString() });
        }

        foreach (var kv in users)
        {
            var user = kv.Value;
            var name = string.IsNullOrWhiteSpace(user.username)
                ? $"{user.first_name} {user.last_name}".Trim()
                : user.username;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = kv.Key.ToString();
            }
            result.Add(new ChatInfo { ChatId = kv.Key, Name = name });
        }

        return result;
    }

    public async Task<InputPeer?> ResolveAsync(long chatId, CancellationToken cancellationToken)
    {
        var (chats, users) = await GetOrRefreshCacheAsync(cancellationToken);
        if (!chats.TryGetValue(chatId, out var chat))
        {
            chats.TryGetValue(ToGetChatsId(chatId), out chat);
        }
        if (users.TryGetValue(chatId, out var user))
        {
            return new InputPeerUser(user.id, user.access_hash);
        }

        if (chat == null)
        {
            return null;
        }

        return chat switch
        {
            Channel channel => new InputPeerChannel(channel.id, channel.access_hash),
            Chat c => new InputPeerChat(c.id),
            _ => null
        };
    }

    public async Task<SourceChatInfo?> GetChatInfoAsync(long chatId, CancellationToken cancellationToken)
    {
        var (chats, users) = await GetOrRefreshCacheAsync(cancellationToken);
        
        if (users.TryGetValue(chatId, out var user))
        {
            var name = string.IsNullOrWhiteSpace(user.username)
                ? $"{user.first_name} {user.last_name}".Trim()
                : user.username;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = chatId.ToString();
            }
            return new SourceChatInfo
            {
                Name = name,
                Username = user.username,
                ChatId = chatId
            };
        }

        if (!chats.TryGetValue(chatId, out var chat))
        {
            chats.TryGetValue(ToGetChatsId(chatId), out chat);
        }

        if (chat == null)
        {
            return null;
        }

        return chat switch
        {
            Channel channel => new SourceChatInfo
            {
                Name = channel.title ?? channel.username ?? chatId.ToString(),
                Username = channel.username,
                ChatId = channel.id
            },
            Chat c => new SourceChatInfo
            {
                Name = c.title ?? chatId.ToString(),
                Username = null,
                ChatId = c.id
            },
            _ => null
        };
    }

    private async Task<(Dictionary<long, ChatBase> Chats, Dictionary<long, User> Users)> GetOrRefreshCacheAsync(CancellationToken cancellationToken)
    {
        await cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (cachedChatsById != null && cachedUsersById != null)
            {
                return (cachedChatsById, cachedUsersById);
            }

            var client = await clientProvider.CreateClientAsync(cancellationToken);
            var dialogs = await client.Messages_GetAllDialogs();
            var users = new Dictionary<long, User>();
            var chats = new Dictionary<long, ChatBase>();
            dialogs.CollectUsersChats(users, chats);

            var chatsById = new Dictionary<long, ChatBase>(chats);
            foreach (var kv in chats)
            {
                if (kv.Value is Channel ch && !chatsById.ContainsKey(ch.id))
                {
                    chatsById[ch.id] = ch;
                }
            }

            cachedChatsById = chatsById;
            cachedUsersById = users;
            return (cachedChatsById, cachedUsersById);
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private static long ToGetChatsId(long peerId)
    {
        if (peerId > 0)
        {
            return -ChannelIdOffset - peerId;
        }

        return peerId;
    }
}
