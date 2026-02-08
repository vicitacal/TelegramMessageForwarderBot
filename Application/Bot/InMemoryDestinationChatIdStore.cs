using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Bot;

public sealed class InMemoryDestinationChatIdStore : IDestinationChatIdStore
{
    private long? chatId;

    public Task<long?> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(chatId);
    }

    public Task SetAsync(long destinationChatId, CancellationToken cancellationToken)
    {
        if (destinationChatId == 0)
        {
            throw new ArgumentException("Destination chat identifier must be non-zero.", nameof(destinationChatId));
        }

        chatId = destinationChatId;
        return Task.CompletedTask;
    }
}
