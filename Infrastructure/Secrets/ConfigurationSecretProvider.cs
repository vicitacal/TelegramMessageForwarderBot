using Microsoft.Extensions.Configuration;
using TelegramMessageForwarder.Application.Secrets;

namespace TelegramMessageForwarder.Infrastructure.Secrets;
#nullable enable

internal class ConfigurationSecretProvider : ISecretProvider {

    public ConfigurationSecretProvider(IConfiguration config) {
        _config = config;
    }

    public string? GetSecret(string name) {
        return _config[name.Replace('.', ':')];
    }

    private readonly IConfiguration _config;

}
