using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Izzy_Moonbot.Service;

public class LoggingService(ILogger<Worker> logger)
{
    private readonly ILogger<Worker> _logger = logger;

    public void Log(string message, SocketCommandContext? context, LogLevel level = LogLevel.Information,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        Log(message, context is not null ? new SocketCommandContextAdapter(context) : null, level, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Log(string message, IIzzyContext? context = null, LogLevel level = LogLevel.Information,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var logMessage = PrepareMessageForLogging(message, context, false, memberName, sourceFilePath, sourceLineNumber);
        _logger.Log(level, logMessage);
    }

    public static string PrepareMessageForLogging(
        string message,
        IIzzyContext? context,
        bool header = false,
        string memberName = "",
        string sourceFilePath = "",
        int sourceLineNumber = 0)
    {
        var logMessage = "";
        if (memberName != "" || sourceFilePath != "" || sourceLineNumber != 0)
        {
            // The whole filepath is overkill, but we do have to account for both Windows and Linux separators here
            var sourceFile = sourceFilePath[(sourceFilePath.LastIndexOf('\\') + 1)..][(sourceFilePath.LastIndexOf('/') + 1)..];
            logMessage += $"[{sourceFile}:{memberName}:{sourceLineNumber}] ";
        }
        if (header)
        {
            logMessage += $"[{DateTime.UtcNow:O}] ";
        }

        if (context != null)
        {
            if (context.IsPrivate)
            {
                logMessage += $"DM with @{context.User.Username} ({context.User.Id})";

                logMessage += ", ";

                logMessage += message;
            }
            else
            {
                logMessage += $"server: {context.Guild?.Name} ({context.Guild?.Id}) #{context.Channel.Name} ({context.Channel.Id})";

                logMessage += " ";

                logMessage += $"@{context.User.Username} ({context.User.Id}), ";
                logMessage += message;
            }
        }
        else
        {
            logMessage += message;
        }

        if (header) logMessage += '\n';

        return logMessage;
    }
}
