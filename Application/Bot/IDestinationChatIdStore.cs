namespace TelegramMessageForwarder.Application.Bot;

public interface IDestinationChatIdStore
{
    long? Get();

    void Set(long chatId);
}
