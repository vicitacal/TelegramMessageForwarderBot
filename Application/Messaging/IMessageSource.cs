using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Messaging;

public interface IMessageSource
{
    Task StartAsync(Func<ChatMessage, CancellationToken, Task> messageHandler, CancellationToken cancellationToken);
}

