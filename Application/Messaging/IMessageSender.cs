using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Messaging;

public interface IMessageSender
{
    Task SendAsync(ChatMessage message, CancellationToken cancellationToken);
}

