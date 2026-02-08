using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Bot;

public sealed class InMemoryAllowedUserIdStore : IAllowedUserIdStore
{
    private readonly ConcurrentDictionary<long, bool> allowedUserIds = new();
    private long? ownerId;

    public InMemoryAllowedUserIdStore(long? initialOwnerId = null)
    {
        if (initialOwnerId.HasValue && initialOwnerId.Value != 0)
        {
            ownerId = initialOwnerId.Value;
            allowedUserIds.TryAdd(initialOwnerId.Value, true);
        }
    }

    public Task<bool> IsAllowedAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(allowedUserIds.ContainsKey(userId));
    }

    public Task SetOwnerIfNotSetAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(userId));
        }

        if (ownerId.HasValue)
        {
            return Task.CompletedTask;
        }

        ownerId = userId;
        allowedUserIds.TryAdd(userId, true);
        return Task.CompletedTask;
    }

    public Task AddAllowedAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(userId));
        }

        allowedUserIds.TryAdd(userId, true);
        return Task.CompletedTask;
    }

    public Task RemoveAllowedAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(userId));
        }

        if (ownerId.HasValue && ownerId.Value == userId)
        {
            throw new InvalidOperationException("Cannot remove the bot owner from the allowed list.");
        }

        allowedUserIds.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task<long?> GetOwnerIdAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ownerId);
    }

    public Task<IReadOnlyCollection<long>> GetAllowedUserIdsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<long>>(allowedUserIds.Keys.ToList());
    }
}
