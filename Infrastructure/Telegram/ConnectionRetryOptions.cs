namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class ConnectionRetryOptions
{
    public const int DefaultMaxRetries = 5;
    public const int DefaultInitialDelayMs = 1000;
    public const int DefaultMaxDelayMs = 60000;
    public const double DefaultBackoffMultiplier = 2.0;

    public ConnectionRetryOptions(
        int maxRetries = DefaultMaxRetries,
        int initialDelayMs = DefaultInitialDelayMs,
        int maxDelayMs = DefaultMaxDelayMs,
        double backoffMultiplier = DefaultBackoffMultiplier)
    {
        if (maxRetries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be at least 1.");
        }

        if (initialDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelayMs), "Initial delay must be non-negative.");
        }

        if (maxDelayMs < initialDelayMs)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelayMs), "Max delay must be at least initial delay.");
        }

        if (backoffMultiplier < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be at least 1.");
        }

        MaxRetries = maxRetries;
        InitialDelayMs = initialDelayMs;
        MaxDelayMs = maxDelayMs;
        BackoffMultiplier = backoffMultiplier;
    }

    public int MaxRetries { get; }

    public int InitialDelayMs { get; }

    public int MaxDelayMs { get; }

    public double BackoffMultiplier { get; }
}
