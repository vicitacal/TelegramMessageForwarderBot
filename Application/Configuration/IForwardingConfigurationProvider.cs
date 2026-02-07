using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Domain.Configuration;

namespace TelegramMessageForwarder.Application.Configuration;

public interface IForwardingConfigurationProvider
{
    Task<ForwardingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken);
}

