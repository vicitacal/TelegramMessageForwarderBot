using System.Collections.Concurrent;
using TelegramMessageForwarder.Domain.Chats;
using TelegramMessageForwarder.Domain.Configuration;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Application.Configuration;

public sealed class InMemoryForwardingConfigurationStore : IForwardingConfigurationProvider, IForwardingConfigurationStore
{
    private readonly ConcurrentDictionary<long, ChatForwardingConfiguration> configurations = new();

    public Task<ForwardingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var list = configurations.Values.ToList();
        var configuration = new ForwardingConfiguration(list);
        return Task.FromResult(configuration);
    }

    public Task AddOrUpdateSourceChatAsync(ChatId sourceChatId, IReadOnlyCollection<string> whitelistWords, IReadOnlyCollection<string> blacklistWords, bool isCaseSensitive, CancellationToken cancellationToken)
    {
        if (sourceChatId.Value == 0)
        {
            throw new ArgumentException("Source chat identifier must be valid.", nameof(sourceChatId));
        }

        var config = new ChatForwardingConfiguration(sourceChatId, whitelistWords ?? Array.Empty<string>(), blacklistWords ?? Array.Empty<string>(), isCaseSensitive);
        configurations[sourceChatId.Value] = config;
        return Task.CompletedTask;
    }

    public Task RemoveSourceChatAsync(ChatId sourceChatId, CancellationToken cancellationToken)
    {
        configurations.TryRemove(sourceChatId.Value, out _);
        return Task.CompletedTask;
    }
}
