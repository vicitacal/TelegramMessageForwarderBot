namespace TelegramMessageForwarder.Application.Commands;

public sealed class Command
{
    public Command(string name, IReadOnlyList<string> arguments)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Command name cannot be null or whitespace.", nameof(name));
        }

        Name = name.TrimStart('/');
        Arguments = arguments ?? Array.Empty<string>();
    }

    public string Name { get; }

    public IReadOnlyList<string> Arguments { get; }
}
