using TelegramMessageForwarder.Application.Secrets;

namespace TelegramMessageForwarder.Infrastructure.Secrets;

public sealed class EnvironmentSecretProvider : ISecretProvider
{
    private readonly string? prefix;

    public EnvironmentSecretProvider(string? prefix = null)
    {
        this.prefix = prefix;
    }

    public string GetSecret(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(name));
        }

        var environmentVariableName = prefix == null ? name : $"{prefix}{name}";

        var value = Environment.GetEnvironmentVariable(environmentVariableName);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Secret '{name}' was not found in environment variables.");
        }

        return value;
    }
}

