using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Bot;

public interface IAllowedUserIdStore
{
    Task<bool> IsAllowedAsync(long userId, CancellationToken cancellationToken);

    Task SetOwnerIfNotSetAsync(long userId, CancellationToken cancellationToken);

    Task AddAllowedAsync(long userId, CancellationToken cancellationToken);

    Task RemoveAllowedAsync(long userId, CancellationToken cancellationToken);

    Task<long?> GetOwnerIdAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<long>> GetAllowedUserIdsAsync(CancellationToken cancellationToken);
}
