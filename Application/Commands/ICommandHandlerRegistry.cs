namespace TelegramMessageForwarder.Application.Commands;

public interface ICommandHandlerRegistry
{
    ICommandHandler? GetHandler(string commandName);
}
