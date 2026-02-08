using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class TelegramInitialOwnerIdProvider : IInitialOwnerIdProvider
{
    private readonly ITelegramClientProvider clientProvider;

    public TelegramInitialOwnerIdProvider(ITelegramClientProvider clientProvider)
    {
        this.clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    public async Task<long?> GetInitialOwnerIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = await clientProvider.CreateClientAsync(cancellationToken);
            var user = await client.LoginUserIfNeeded();
            if (user?.id == 0)
            {
                return null;
            }

            return user!.id;
        }
        catch
        {
            return null;
        }
    }
}
