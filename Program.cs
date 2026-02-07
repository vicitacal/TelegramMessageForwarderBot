using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Commands;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Application.Messages;
using TelegramMessageForwarder.Application.Secrets;
using TelegramMessageForwarder.Infrastructure.Bot;
using TelegramMessageForwarder.Infrastructure.Secrets;
using TelegramMessageForwarder.Infrastructure.Telegram;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();
builder.Services.AddSingleton<IDestinationChatIdStore, InMemoryDestinationChatIdStore>();
builder.Services.AddSingleton<ConnectionRetryOptions>();
builder.Services.AddSingleton<ITelegramClientProvider, TelegramClientProvider>();
builder.Services.AddSingleton<IMessageSource, TelegramMessageSource>();
builder.Services.AddSingleton<IMessageSender, BotApiMessageSender>();
builder.Services.AddSingleton<IResponseSender>(sp => (IResponseSender)sp.GetRequiredService<IMessageSender>());
builder.Services.AddSingleton<IBotUpdateReceiver, TelegramBotUpdateReceiver>();
builder.Services.AddSingleton<IMessageProcessingUseCase, MessageProcessingUseCase>();
builder.Services.AddSingleton<ICommandParser, CommandParser>();
builder.Services.AddSingleton<ICommandHandler, StartCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, HelpCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, AddSourceCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, ListSourcesCommandHandler>();
builder.Services.AddSingleton<ICommandHandlerRegistry>(sp =>
{
    var handlers = sp.GetServices<ICommandHandler>().ToArray();
    return new CommandHandlerRegistry(handlers);
});
builder.Services.AddSingleton<InMemoryForwardingConfigurationStore>();
builder.Services.AddSingleton<IForwardingConfigurationProvider>(sp => sp.GetRequiredService<InMemoryForwardingConfigurationStore>());
builder.Services.AddSingleton<IForwardingConfigurationStore>(sp => sp.GetRequiredService<InMemoryForwardingConfigurationStore>());
builder.Services.AddSingleton<IMessageForwardingOrchestrator, MessageForwardingOrchestrator>();

var host = builder.Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var orchestrator = host.Services.GetRequiredService<IMessageForwardingOrchestrator>();

await orchestrator.RunAsync(cts.Token);
