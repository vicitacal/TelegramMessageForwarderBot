using TelegramMessageForwarder.Domain.Configuration;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Messages;

public interface IMessageProcessingUseCase
{
    MessageProcessingResult Process(ChatMessage message, ForwardingConfiguration configuration);
}

