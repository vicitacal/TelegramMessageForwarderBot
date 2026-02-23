using TelegramMessageForwarder.Domain.Messages;

namespace TelegramMessageForwarder.Application.Commands;

public sealed class CommandParser : ICommandParser
{
    private const char CommandPrefix = '/';

    public bool TryParse(ChatMessage message, out Command? command)
    {
        command = null;

        var text = message.Text.Value;
        if (string.IsNullOrWhiteSpace(text) || text[0] != CommandPrefix)
        {
            return false;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var name = parts[0].TrimStart(CommandPrefix);
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var arguments = parts.Length > 1 ? parts[1..].Select(ReplaceWhiteSpaces).ToArray() : Array.Empty<string>();
        command = new Command(name, arguments);
        return true;
    }

    private string ReplaceWhiteSpaces(string arg) {
        var escaped = arg.Replace(@"\_", "&unLine;");
        var spaced = escaped.Replace('_', ' ');
        return spaced.Replace("&unLine;", "_");
    }

}
