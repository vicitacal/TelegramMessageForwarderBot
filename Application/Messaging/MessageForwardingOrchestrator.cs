using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Commands;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messages;

namespace TelegramMessageForwarder.Application.Messaging;

public sealed class MessageForwardingOrchestrator : IMessageForwardingOrchestrator
{
    private readonly IMessageSource messageSource;
    private readonly IMessageSender messageSender;
    private readonly IMessageProcessingUseCase messageProcessingUseCase;
    private readonly IForwardingConfigurationProvider configurationProvider;
    private readonly IBotUpdateReceiver botUpdateReceiver;
    private readonly ICommandParser commandParser;
    private readonly ICommandHandlerRegistry commandHandlerRegistry;

    public MessageForwardingOrchestrator(
        IMessageSource messageSource,
        IMessageSender messageSender,
        IMessageProcessingUseCase messageProcessingUseCase,
        IForwardingConfigurationProvider configurationProvider,
        IBotUpdateReceiver botUpdateReceiver,
        ICommandParser commandParser,
        ICommandHandlerRegistry commandHandlerRegistry)
    {
        this.messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
        this.messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        this.messageProcessingUseCase = messageProcessingUseCase ?? throw new ArgumentNullException(nameof(messageProcessingUseCase));
        this.configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        this.botUpdateReceiver = botUpdateReceiver ?? throw new ArgumentNullException(nameof(botUpdateReceiver));
        this.commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        this.commandHandlerRegistry = commandHandlerRegistry ?? throw new ArgumentNullException(nameof(commandHandlerRegistry));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var botTask = botUpdateReceiver.StartAsync(
            async (botUpdate, ct) =>
            {
                var message = botUpdate.Message;
                if (!commandParser.TryParse(message, out var command) || command == null)
                {
                    return;
                }

                var handler = commandHandlerRegistry.GetHandler(command.Name);
                if (handler != null)
                {
                    await handler.HandleAsync(command, message, ct);
                }
            },
            cancellationToken);

        var forwardTask = messageSource.StartAsync(
            async (message, ct) =>
            {
                var configuration = await configurationProvider.GetConfigurationAsync(ct);
                var result = messageProcessingUseCase.Process(message, configuration);

                if (!result.ShouldBeForwarded)
                {
                    return;
                }

                await messageSender.SendAsync(message, ct);
            },
            cancellationToken);

        await Task.WhenAll(botTask, forwardTask);
    }
}

