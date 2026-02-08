using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Configuration;

public interface IInitialOwnerIdProvider
{
    Task<long?> GetInitialOwnerIdAsync(CancellationToken cancellationToken);
}
