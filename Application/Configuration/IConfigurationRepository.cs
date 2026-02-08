using System.Threading;
using System.Threading.Tasks;

namespace TelegramMessageForwarder.Application.Configuration;

public interface IConfigurationRepository
{
    Task<ConfigurationFile> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ConfigurationFile data, CancellationToken cancellationToken);
}
