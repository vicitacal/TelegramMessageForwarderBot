namespace TelegramMessageForwarder.Application.Commands;

public sealed class CommandHandlerRegistry : ICommandHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, ICommandHandler> handlers;

    public CommandHandlerRegistry(IEnumerable<ICommandHandler> handlers)
    {
        if (handlers == null)
        {
            throw new ArgumentNullException(nameof(handlers));
        }

        var dictionary = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase);

        foreach (var handler in handlers)
        {
            if (handler == null)
            {
                throw new ArgumentException("Command handlers cannot contain null values.", nameof(handlers));
            }

            if (dictionary.ContainsKey(handler.CommandName))
            {
                throw new ArgumentException($"Duplicate handler for command '{handler.CommandName}'.", nameof(handlers));
            }

            dictionary.Add(handler.CommandName, handler);
        }

        this.handlers = dictionary;
    }

    public ICommandHandler? GetHandler(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        return handlers.TryGetValue(commandName.Trim(), out var handler) ? handler : null;
    }
}
