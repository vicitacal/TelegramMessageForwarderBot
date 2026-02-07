using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Domain.Chats;
using TelegramMessageForwarder.Domain.Configuration;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Application.Configuration;

public interface IForwardingConfigurationStore
{
    Task<ForwardingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken);

    Task AddOrUpdateSourceChatAsync(ChatId sourceChatId, IReadOnlyCollection<string> whitelistWords, IReadOnlyCollection<string> blacklistWords, bool isCaseSensitive, CancellationToken cancellationToken);

    Task RemoveSourceChatAsync(ChatId sourceChatId, CancellationToken cancellationToken);
}
