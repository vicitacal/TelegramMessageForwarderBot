namespace TelegramMessageForwarder.Application.Secrets;

public interface ISecretProvider
{
    string? GetSecret(string name);
}

