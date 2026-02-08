using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramMessageForwarder.Application.Bot;
using TelegramMessageForwarder.Application.Messaging;
using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class UsersCommandHandler : ICommandHandler
{
    private const string UsersCommandName = "users";
    private const string ListSubcommand = "list";
    private const string AddSubcommand = "add";
    private const string RemoveSubcommand = "remove";

    private static readonly string UsageMessage = $"Usage: /users {ListSubcommand} | /users {AddSubcommand} <user_id> | /users {RemoveSubcommand} <user_id>";

    private readonly IResponseSender responseSender;
    private readonly IAllowedUserIdStore allowedUserIdStore;

    public UsersCommandHandler(IResponseSender responseSender, IAllowedUserIdStore allowedUserIdStore)
    {
        this.responseSender = responseSender ?? throw new ArgumentNullException(nameof(responseSender));
        this.allowedUserIdStore = allowedUserIdStore ?? throw new ArgumentNullException(nameof(allowedUserIdStore));
    }

    public string CommandName => UsersCommandName;

    public async Task HandleAsync(Command command, ChatMessage message, CancellationToken cancellationToken)
    {
        if (command.Arguments.Count == 0)
        {
            await responseSender.SendAsync(UsageMessage, cancellationToken);
            return;
        }

        var subcommand = command.Arguments[0].Trim().ToLowerInvariant();
        if (subcommand != ListSubcommand && subcommand != AddSubcommand && subcommand != RemoveSubcommand)
        {
            await responseSender.SendAsync(UsageMessage, cancellationToken);
            return;
        }

        if (subcommand == ListSubcommand)
        {
            await HandleListAsync(cancellationToken);
            return;
        }

        if (command.Arguments.Count < 2)
        {
            await responseSender.SendAsync($"Usage: /users {subcommand} <user_id>", cancellationToken);
            return;
        }

        if (!long.TryParse(command.Arguments[1], out var userId) || userId == 0)
        {
            await responseSender.SendAsync($"Invalid user ID. Usage: /users {subcommand} <user_id>", cancellationToken);
            return;
        }

        if (subcommand == AddSubcommand)
        {
            await HandleAddAsync(userId, cancellationToken);
        }
        else
        {
            await HandleRemoveAsync(userId, cancellationToken);
        }
    }

    private async Task HandleListAsync(CancellationToken cancellationToken)
    {
        var ownerId = await allowedUserIdStore.GetOwnerIdAsync(cancellationToken);
        var allowed = await allowedUserIdStore.GetAllowedUserIdsAsync(cancellationToken);

        if (allowed.Count == 0)
        {
            await responseSender.SendAsync("No allowed users. Send /start to become the owner.", cancellationToken);
            return;
        }

        var lines = allowed.Select(id => ownerId.HasValue && id == ownerId.Value ? $"{id} (owner)" : id.ToString());
        var text = "Allowed user IDs:\n" + string.Join("\n", lines);
        await responseSender.SendAsync(text, cancellationToken);
    }

    private async Task HandleAddAsync(long userId, CancellationToken cancellationToken)
    {
        await allowedUserIdStore.AddAllowedAsync(userId, cancellationToken);
        await responseSender.SendAsync($"User {userId} is now allowed to use the bot.", cancellationToken);
    }

    private async Task HandleRemoveAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            await allowedUserIdStore.RemoveAllowedAsync(userId, cancellationToken);
            await responseSender.SendAsync($"User {userId} is no longer allowed to use the bot.", cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await responseSender.SendAsync("The bot owner cannot be removed from the allowed list.", cancellationToken);
        }
    }
}
