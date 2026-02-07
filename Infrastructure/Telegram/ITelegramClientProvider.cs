using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public interface ITelegramClientProvider
{
    Task<Client> CreateClientAsync(CancellationToken cancellationToken);
}

