using TelegramMessageForwarder.Domain.Chats;
using TelegramMessageForwarder.Domain.Messages;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Domain.Configuration;

public sealed class ForwardingConfiguration
{
    private readonly IReadOnlyCollection<ChatForwardingConfiguration> chatConfigurations;

    public ForwardingConfiguration(IReadOnlyCollection<ChatForwardingConfiguration> chatConfigurations)
    {
        if (chatConfigurations == null)
        {
            throw new ArgumentNullException(nameof(chatConfigurations));
        }

        if (chatConfigurations.Any(configuration => configuration == null))
        {
            throw new ArgumentException("Chat configurations cannot contain null values.", nameof(chatConfigurations));
        }

        var seenSourceChatIds = new HashSet<ChatId>();
        foreach (var configuration in chatConfigurations)
        {
            if (!seenSourceChatIds.Add(configuration.SourceChatId))
            {
                throw new ArgumentException("Duplicate configuration for the same source chat identifier.", nameof(chatConfigurations));
            }
        }

        this.chatConfigurations = chatConfigurations.ToArray();
    }

    public IReadOnlyCollection<ChatForwardingConfiguration> GetChatConfigurations()
    {
        return chatConfigurations;
    }

    public bool ShouldForward(ChatMessage message)
    {
        var decision = Decide(message);

        return decision.ShouldForward;
    }

    public ForwardingDecision Decide(ChatMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var configuration = chatConfigurations.FirstOrDefault(c => c.SourceChatId.Equals(message.ChatId));
        if (configuration == null)
        {
            return new ForwardingDecision(false, ForwardingDecisionReason.NoConfiguration);
        }

        return configuration.Decide(message);
    }
}

