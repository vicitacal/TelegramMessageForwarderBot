using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public interface ICommandParser
{
    bool TryParse(ChatMessage message, out Command? command);
}
