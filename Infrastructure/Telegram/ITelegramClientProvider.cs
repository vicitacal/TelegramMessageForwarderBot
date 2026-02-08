using System.Threading;
using System.Threading.Tasks;
using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public interface ITelegramClientProvider
{
    Task<Client> CreateClientAsync(CancellationToken cancellationToken);

    Task InvalidateClientAsync(CancellationToken cancellationToken);
}

