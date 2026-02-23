using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Messaging;

public interface IResponseSender
{
    Task SendAsync(string text, CancellationToken cancellationToken);

    Task SendAsync(BotResponse response, CancellationToken cancellationToken);

    Task SendToChatAsync(string text, long chatId, CancellationToken cancellationToken);

    Task SendToChatAsync(BotResponse response, long chatId, CancellationToken cancellationToken);
}
