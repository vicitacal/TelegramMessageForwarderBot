using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Commands;
using TelegramMessageForwarder.Application.Configuration;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Application.Messages;
using TelegramMessageForwarder.Application.Secrets;
using TelegramMessageForwarder.Infrastructure.Bot;
using TelegramMessageForwarder.Infrastructure.Configuration;
using TelegramMessageForwarder.Infrastructure.Secrets;
using TelegramMessageForwarder.Infrastructure.Telegram;
using TelegramMessageForwarder.Application.Chats;

var builder = Host.CreateApplicationBuilder(args);

const string DefaultConfigFilePath = "config.json";
var configFilePath = args.Length > 0 ? args[0] : DefaultConfigFilePath;

builder.Services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();
builder.Services.AddSingleton<IInitialOwnerIdProvider, TelegramInitialOwnerIdProvider>();
builder.Services.AddSingleton<IConfigurationRepository>(sp =>
{
    var initialOwnerIdProvider = sp.GetRequiredService<IInitialOwnerIdProvider>();
    var secretProvider = sp.GetRequiredService<ISecretProvider>();
    return new FileConfigurationRepository(configFilePath, initialOwnerIdProvider, secretProvider);
});
builder.Services.AddSingleton<IAllowedUserIdStore, FileBackedAllowedUserIdStore>();
builder.Services.AddSingleton<IDestinationChatIdStore, FileBackedDestinationChatIdStore>();
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
builder.Services.AddSingleton<ICommandHandler, UsersCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, SourcesCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, WhitelistCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, BlacklistCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, ListChatsCommandHandler>();
builder.Services.AddSingleton<IChatListProvider, TelegramChatListProvider>();
builder.Services.AddSingleton<ICommandHandlerRegistry>(sp =>
{
    var handlers = sp.GetServices<ICommandHandler>().ToArray();
    return new CommandHandlerRegistry(handlers);
});
builder.Services.AddSingleton<FileBackedForwardingConfigurationStore>();
builder.Services.AddSingleton<IForwardingConfigurationProvider>(sp => sp.GetRequiredService<FileBackedForwardingConfigurationStore>());
builder.Services.AddSingleton<IForwardingConfigurationStore>(sp => sp.GetRequiredService<FileBackedForwardingConfigurationStore>());
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
