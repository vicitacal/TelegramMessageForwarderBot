namespace TelegramMessageForwarder.Domain.Configuration;

public enum ForwardingDecisionReason
{
    NoConfiguration,
    Whitelisted,
    Blacklisted,
    DefaultForward
}

