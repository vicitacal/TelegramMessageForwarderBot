using TelegramMessageForwarder.Application.Secrets;

namespace TelegramMessageForwarder.Infrastructure.Secrets;

public sealed class EnvironmentSecretProvider : ISecretProvider
{
    
    public EnvironmentSecretProvider()
    {

    }

    public string? GetSecret(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(name));
        }

        return Environment.GetEnvironmentVariable(name) ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
    }
}

