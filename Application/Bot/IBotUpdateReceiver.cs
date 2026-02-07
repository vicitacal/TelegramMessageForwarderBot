using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Bot;

public interface IBotUpdateReceiver
{
    Task StartAsync(Func<BotUpdate, CancellationToken, Task> updateHandler, CancellationToken cancellationToken);
}
