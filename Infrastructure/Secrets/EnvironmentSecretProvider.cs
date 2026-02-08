using TelegramMessageForwarder.Application.Secrets;

namespace TelegramMessageForwarder.Infrastructure.Secrets;

public sealed class EnvironmentSecretProvider : ISecretProvider
{
    private readonly string? prefix;

    public EnvironmentSecretProvider(string? prefix = null)
    {
        this.prefix = prefix;
    }

    public string? GetSecret(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(name));
        }

        var environmentVariableName = prefix == null ? name : $"{prefix}{name}";

        return Environment.GetEnvironmentVariable(environmentVariableName, EnvironmentVariableTarget.User);
    }
}

