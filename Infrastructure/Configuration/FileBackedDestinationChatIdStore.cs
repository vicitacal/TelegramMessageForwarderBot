using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Configuration;

namespace TelegramMessageForwarder.Infrastructure.Configuration;

public sealed class FileBackedDestinationChatIdStore : IDestinationChatIdStore
{
    private readonly IConfigurationRepository repository;

    public FileBackedDestinationChatIdStore(IConfigurationRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<long?> GetAsync(CancellationToken cancellationToken)
    {
        var data = await repository.LoadAsync(cancellationToken);
        return data.DestinationChatId;
    }

    public async Task SetAsync(long chatId, CancellationToken cancellationToken)
    {
        if (chatId == 0)
        {
            throw new ArgumentException("Destination chat identifier must be non-zero.", nameof(chatId));
        }

        var data = await repository.LoadAsync(cancellationToken);
        data.DestinationChatId = chatId;
        await repository.SaveAsync(data, cancellationToken);
    }
}
