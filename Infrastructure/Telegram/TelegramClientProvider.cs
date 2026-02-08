using NLog;
using TelegramMessageForwarder.Application.Secrets;
using WTelegram;

namespace TelegramMessageForwarder.Infrastructure.Telegram;

public sealed class TelegramClientProvider : ITelegramClientProvider, IAsyncDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string TelegramApiIdKey = "Telegram.ApiId";
    private const string TelegramApiHashKey = "Telegram.ApiHash";
    private const string TelegramPhoneNumberKey = "Telegram.PhoneNumber";
    private const string TelegramVerificationCodeKey = "Telegram.VerificationCode";
    private const string TelegramPasswordKey = "Telegram.Password";
    private const string TelegramSessionPathKey = "Telegram.SessionPathname";

    private readonly ISecretProvider secretProvider;
    private readonly ConnectionRetryOptions retryOptions;
    private readonly SemaphoreSlim clientLock = new(1, 1);
    private Client? sharedClient;

    public TelegramClientProvider(ISecretProvider secretProvider, ConnectionRetryOptions? retryOptions = null)
    {
        this.secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
        this.retryOptions = retryOptions ?? new ConnectionRetryOptions();
    }

    public async Task<Client> CreateClientAsync(CancellationToken cancellationToken)
    {
        await clientLock.WaitAsync(cancellationToken);

        try
        {
            if (sharedClient != null)
            {
                return sharedClient;
            }

            var delayMs = retryOptions.InitialDelayMs;
            Exception? lastException = null;

            for (var attempt = 1; attempt <= retryOptions.MaxRetries; attempt++)
            {
                try
                {
                    Logger.Info("Connecting to Telegram (attempt {Attempt}/{MaxRetries}).", attempt, retryOptions.MaxRetries);

                    var client = new Client(GetConfigValue);
                    await client.LoginUserIfNeeded();

                    Logger.Info("Connected to Telegram.");
                    sharedClient = client;
                    return sharedClient;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Logger.Warn(ex, "Telegram connection attempt {Attempt}/{MaxRetries} failed.", attempt, retryOptions.MaxRetries);

                    if (attempt == retryOptions.MaxRetries)
                    {
                        break;
                    }

                    var delay = TimeSpan.FromMilliseconds(Math.Min(delayMs, retryOptions.MaxDelayMs));
                    Logger.Info("Retrying in {DelayMs} ms.", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                    delayMs = (int)(delayMs * retryOptions.BackoffMultiplier);
                }
            }

            throw new InvalidOperationException("Failed to connect to Telegram after all retries.", lastException);
        }
        finally
        {
            clientLock.Release();
        }
    }

    public async Task InvalidateClientAsync(CancellationToken cancellationToken)
    {
        await clientLock.WaitAsync(cancellationToken);

        try
        {
            if (sharedClient == null)
            {
                return;
            }

            try
            {
                await sharedClient.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error disposing Telegram client.");
            }

            sharedClient = null;
        }
        finally
        {
            clientLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await InvalidateClientAsync(CancellationToken.None);
    }

    private string? GetConfigValue(string name)
    {
        return name switch
        {
            "api_id" => secretProvider.GetSecret(TelegramApiIdKey),
            "api_hash" => secretProvider.GetSecret(TelegramApiHashKey),
            "phone_number" => secretProvider.GetSecret(TelegramPhoneNumberKey),
            "verification_code" => secretProvider.GetSecret(TelegramVerificationCodeKey),
            "password" => secretProvider.GetSecret(TelegramPasswordKey),
            "session_pathname" => secretProvider.GetSecret(TelegramSessionPathKey),
            _ => null
        };
    }
}
