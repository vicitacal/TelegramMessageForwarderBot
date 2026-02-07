using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public interface ICommandHandler
{
    string CommandName { get; }

    Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken);
}
