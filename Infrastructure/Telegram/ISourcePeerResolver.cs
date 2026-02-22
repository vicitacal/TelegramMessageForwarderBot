using System.Threading;
using System.Threading.Tasks;
using TL;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public interface ISourcePeerResolver
{
    Task<InputPeer?> ResolveAsync(long chatId, CancellationToken cancellationToken);
    
    Task<SourceChatInfo?> GetChatInfoAsync(long chatId, CancellationToken cancellationToken);
}

public sealed class SourceChatInfo
{
    public required string Name { get; init; }
    public string? Username { get; init; }
    public long ChatId { get; init; }
}
