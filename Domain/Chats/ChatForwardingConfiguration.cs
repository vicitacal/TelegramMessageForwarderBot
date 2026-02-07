using TelegramMessageForwarder.Domain.Configuration;
using TelegramMessageForwarder.Domain.Messages;
using TelegramMessageForwarder.Domain.ValueObjects;

namespace TelegramMessageForwarder.Domain.Chats;

public sealed class ChatForwardingConfiguration
{
    private readonly IReadOnlyCollection<string> whitelistWords;
    private readonly IReadOnlyCollection<string> blacklistWords;

    public ChatForwardingConfiguration(
        ChatId sourceChatId,
        IReadOnlyCollection<string> whitelistWords,
        IReadOnlyCollection<string> blacklistWords,
        bool isCaseSensitive)
    {
        if (sourceChatId.Value == 0)
        {
            throw new ArgumentException("Source chat identifier must be valid.", nameof(sourceChatId));
        }

        SourceChatId = sourceChatId;

        this.whitelistWords = NormalizeWords(whitelistWords);
        this.blacklistWords = NormalizeWords(blacklistWords);

        IsCaseSensitive = isCaseSensitive;
    }

    public ChatId SourceChatId { get; }

    public bool IsCaseSensitive { get; }

    public IReadOnlyCollection<string> GetWhitelistWords()
    {
        return whitelistWords;
    }

    public IReadOnlyCollection<string> GetBlacklistWords()
    {
        return blacklistWords;
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

        if (!message.ChatId.Equals(SourceChatId))
        {
            return new ForwardingDecision(true, ForwardingDecisionReason.DefaultForward);
        }

        var text = message.Text.Value;

        if (MatchesAnyWord(text, whitelistWords, IsCaseSensitive))
        {
            return new ForwardingDecision(true, ForwardingDecisionReason.Whitelisted);
        }

        if (MatchesAnyWord(text, blacklistWords, IsCaseSensitive))
        {
            return new ForwardingDecision(false, ForwardingDecisionReason.Blacklisted);
        }

        return new ForwardingDecision(true, ForwardingDecisionReason.DefaultForward);
    }

    private static IReadOnlyCollection<string> NormalizeWords(IReadOnlyCollection<string> words)
    {
        if (words == null || words.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>(words.Count);

        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            var trimmed = word.Trim();

            normalized.Add(trimmed);
        }

        return normalized.AsReadOnly();
    }

    private static bool MatchesAnyWord(string text, IReadOnlyCollection<string> words, bool isCaseSensitive)
    {
        if (words.Count == 0)
        {
            return false;
        }

        foreach (var word in words)
        {
            if (word.Length == 0)
            {
                continue;
            }

            var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (text.IndexOf(word, comparison) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}

