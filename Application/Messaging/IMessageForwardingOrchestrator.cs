using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Messaging;

public interface IMessageForwardingOrchestrator
{
    Task RunAsync(CancellationToken cancellationToken);
}

