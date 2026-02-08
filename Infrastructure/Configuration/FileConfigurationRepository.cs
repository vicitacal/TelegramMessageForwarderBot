using Newtonsoft.Json;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Secrets;

namespace TelegramMessageForwarder.Infrastructure.Configuration;

public sealed class FileConfigurationRepository : IConfigurationRepository
{
    private const string BotOwnerIdSecretKey = "Telegram.BotOwnerId";

    private readonly string filePath;
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private readonly IInitialOwnerIdProvider initialOwnerIdProvider;
    private readonly ISecretProvider secretProvider;

    public FileConfigurationRepository(
        string filePath,
        IInitialOwnerIdProvider initialOwnerIdProvider,
        ISecretProvider secretProvider)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Configuration file path cannot be null or whitespace.", nameof(filePath));
        }

        this.filePath = filePath;
        this.initialOwnerIdProvider = initialOwnerIdProvider ?? throw new ArgumentNullException(nameof(initialOwnerIdProvider));
        this.secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
    }

    public async Task<ConfigurationFile> LoadAsync(CancellationToken cancellationToken)
    {
        await fileLock.WaitAsync(cancellationToken);

        try
        {
            ConfigurationFile data;

            if (!File.Exists(filePath))
            {
                data = new ConfigurationFile();
            }
            else
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                data = JsonConvert.DeserializeObject<ConfigurationFile>(json) ?? new ConfigurationFile();
            }

            data.AllowedUserIds ??= new List<long>();
            data.ChatConfigurations ??= new List<ChatConfigurationEntry>();

            if (!data.OwnerId.HasValue)
            {
                var ownerId = await initialOwnerIdProvider.GetInitialOwnerIdAsync(cancellationToken);
                if (!ownerId.HasValue)
                {
                    ownerId = ParseOwnerIdFromSecret(secretProvider.GetSecret(BotOwnerIdSecretKey));
                }

                if (ownerId.HasValue)
                {
                    data.OwnerId = ownerId.Value;
                    if (!data.AllowedUserIds.Contains(ownerId.Value))
                    {
                        data.AllowedUserIds.Add(ownerId.Value);
                    }
                    await SaveInternalAsync(data, cancellationToken);
                }
            }

            return data;
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task SaveAsync(ConfigurationFile data, CancellationToken cancellationToken)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        await fileLock.WaitAsync(cancellationToken);

        try
        {
            await SaveInternalAsync(data, cancellationToken);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private static long? ParseOwnerIdFromSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !long.TryParse(value.Trim(), out var id) || id == 0)
        {
            return null;
        }

        return id;
    }

    private async Task SaveInternalAsync(ConfigurationFile data, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }
}
