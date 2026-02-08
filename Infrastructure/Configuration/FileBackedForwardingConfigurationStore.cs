using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Domain.Chats;
using TelegramMessageForwarder.Domain.Configuration;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Infrastructure.Configuration;

public sealed class FileBackedForwardingConfigurationStore : IForwardingConfigurationProvider, IForwardingConfigurationStore
{
    private readonly IConfigurationRepository repository;

    public FileBackedForwardingConfigurationStore(IConfigurationRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ForwardingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var data = await repository.LoadAsync(cancellationToken);
        var chatConfigurations = data.ChatConfigurations
            .Select(e => new ChatForwardingConfiguration(
                new ChatId(e.SourceChatId),
                e.WhitelistWords ?? new List<string>(),
                e.BlacklistWords ?? new List<string>(),
                e.IsCaseSensitive))
            .ToList();
        return new ForwardingConfiguration(chatConfigurations);
    }

    public async Task AddOrUpdateSourceChatAsync(ChatId sourceChatId, IReadOnlyCollection<string> whitelistWords, IReadOnlyCollection<string> blacklistWords, bool isCaseSensitive, CancellationToken cancellationToken)
    {
        if (sourceChatId.Value == 0)
        {
            throw new ArgumentException("Source chat identifier must be valid.", nameof(sourceChatId));
        }

        var data = await repository.LoadAsync(cancellationToken);
        var entry = data.ChatConfigurations.FirstOrDefault(c => c.SourceChatId == sourceChatId.Value);
        if (entry != null)
        {
            data.ChatConfigurations.Remove(entry);
        }

        data.ChatConfigurations.Add(new ChatConfigurationEntry
        {
            SourceChatId = sourceChatId.Value,
            WhitelistWords = (whitelistWords ?? Array.Empty<string>()).ToList(),
            BlacklistWords = (blacklistWords ?? Array.Empty<string>()).ToList(),
            IsCaseSensitive = isCaseSensitive
        });

        await repository.SaveAsync(data, cancellationToken);
    }

    public async Task RemoveSourceChatAsync(ChatId sourceChatId, CancellationToken cancellationToken)
    {
        var data = await repository.LoadAsync(cancellationToken);
        var removed = data.ChatConfigurations.RemoveAll(c => c.SourceChatId == sourceChatId.Value);
        if (removed > 0)
        {
            await repository.SaveAsync(data, cancellationToken);
        }
    }
}
