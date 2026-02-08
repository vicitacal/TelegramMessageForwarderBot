using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Chats;
using TL;
using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class TelegramChatListProvider : IChatListProvider
{
    private readonly ITelegramClientProvider clientProvider;

    public TelegramChatListProvider(ITelegramClientProvider clientProvider)
    {
        this.clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    public async Task<IReadOnlyList<ChatInfo>> GetChatsAsync(CancellationToken cancellationToken)
    {
        var client = await clientProvider.CreateClientAsync(cancellationToken);
        var dialogs = await client.Messages_GetAllDialogs();
        var users = new Dictionary<long, User>();
        var chats = new Dictionary<long, ChatBase>();
        dialogs.CollectUsersChats(users, chats);

        var result = new List<ChatInfo>(chats.Count + users.Count);

        foreach (var kv in chats)
        {
            var name = kv.Value switch
            {
                Channel channel => channel.title ?? channel.username ?? kv.Key.ToString(),
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
}
