using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Chats;

public interface IChatListProvider
{
    Task<IReadOnlyList<ChatInfo>> GetChatsAsync(CancellationToken cancellationToken);
}
