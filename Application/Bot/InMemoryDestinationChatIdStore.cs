namespace TelegramMessageForwarder.Application.Bot;

public sealed class InMemoryDestinationChatIdStore : IDestinationChatIdStore
{
    private long? chatId;

    public long? Get()
    {
        return chatId;
    }

    public void Set(long destinationChatId)
    {
        if (destinationChatId == 0)
        {
            throw new ArgumentException("Destination chat identifier must be non-zero.", nameof(destinationChatId));
        }

        chatId = destinationChatId;
    }
}
