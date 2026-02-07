namespace TelegramMessageForwarder.Domain.Configuration;

public sealed class ForwardingDecision
{
    public ForwardingDecision(bool shouldForward, ForwardingDecisionReason reason)
    {
        ShouldForward = shouldForward;
        Reason = reason;
    }

    public bool ShouldForward { get; }

    public ForwardingDecisionReason Reason { get; }
}

