using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Bot;

public interface IDestinationChatIdStore
{
    Task<long?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(long chatId, CancellationToken cancellationToken);
}
