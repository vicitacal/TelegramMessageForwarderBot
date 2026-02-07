using TelegramMessageForwarder.Domain.Configuration;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Messages;

public sealed class MessageProcessingUseCase : IMessageProcessingUseCase
{
    public MessageProcessingResult Process(ChatMessage message, ForwardingConfiguration configuration)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var shouldBeForwarded = configuration.ShouldForward(message);

        return new MessageProcessingResult(message, shouldBeForwarded);
    }
}

