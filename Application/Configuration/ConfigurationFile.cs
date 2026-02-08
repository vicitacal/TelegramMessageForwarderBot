namespace TelegramMessageForwarder.Application.Configuration;

public sealed class ConfigurationFile
{
    public long? OwnerId { get; set; }

    public List<long> AllowedUserIds { get; set; } = new();

    public long? DestinationChatId { get; set; }

    public List<ChatConfigurationEntry> ChatConfigurations { get; set; } = new();
}

public sealed class ChatConfigurationEntry
{
    public long SourceChatId { get; set; }

    public List<string> WhitelistWords { get; set; } = new();

    public List<string> BlacklistWords { get; set; } = new();

    public bool IsCaseSensitive { get; set; }
}
