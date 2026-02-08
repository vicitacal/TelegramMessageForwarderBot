using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Configuration;

namespace TelegramMessageForwarder.Infrastructure.Configuration;

public sealed class FileBackedAllowedUserIdStore : IAllowedUserIdStore
{
    private readonly IConfigurationRepository repository;

    public FileBackedAllowedUserIdStore(IConfigurationRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<bool> IsAllowedAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            return false;
        }

        var data = await repository.LoadAsync(cancellationToken);
        return data.AllowedUserIds.Contains(userId);
    }

    public async Task SetOwnerIfNotSetAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(userId));
        }

        var data = await repository.LoadAsync(cancellationToken);
        if (data.OwnerId.HasValue)
        {
            return;
        }

        data.OwnerId = userId;
        if (!data.AllowedUserIds.Contains(userId))
        {
            data.AllowedUserIds.Add(userId);
        }

        await repository.SaveAsync(data, cancellationToken);
    }

    public async Task AddAllowedAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(userId));
        }

        var data = await repository.LoadAsync(cancellationToken);
        if (!data.AllowedUserIds.Contains(userId))
        {
            data.AllowedUserIds.Add(userId);
            await repository.SaveAsync(data, cancellationToken);
        }
    }

    public async Task RemoveAllowedAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            throw new ArgumentException("User identifier must be non-zero.", nameof(userId));
        }

        var data = await repository.LoadAsync(cancellationToken);
        if (data.OwnerId.HasValue && data.OwnerId.Value == userId)
        {
            throw new InvalidOperationException("Cannot remove the bot owner from the allowed list.");
        }

        if (data.AllowedUserIds.Remove(userId))
        {
            await repository.SaveAsync(data, cancellationToken);
        }
    }

    public async Task<long?> GetOwnerIdAsync(CancellationToken cancellationToken)
    {
        var data = await repository.LoadAsync(cancellationToken);
        return data.OwnerId;
    }

    public async Task<IReadOnlyCollection<long>> GetAllowedUserIdsAsync(CancellationToken cancellationToken)
    {
        var data = await repository.LoadAsync(cancellationToken);
        return data.AllowedUserIds.AsReadOnly();
    }
}
