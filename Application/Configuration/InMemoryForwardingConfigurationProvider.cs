using TelegramMessageForwarder.Domain.Chats;
using TelegramMessageForwarder.Domain.Configuration;

namespace TelegramMessageForwarder.Application.Configuration;

public sealed class InMemoryForwardingConfigurationProvider : IForwardingConfigurationProvider
{
    public Task<ForwardingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var configuration = new ForwardingConfiguration(Array.Empty<ChatForwardingConfiguration>());
        return Task.FromResult(configuration);
    }
}
