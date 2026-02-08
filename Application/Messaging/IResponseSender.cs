using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Messaging;

public interface IResponseSender
{
    Task SendAsync(string text, CancellationToken cancellationToken);

    Task SendToChatAsync(string text, long chatId, CancellationToken cancellationToken);
}
