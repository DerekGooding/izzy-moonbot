using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using System.Linq;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Attributes;

// Moderation only commands
// Used for sensitive moderation commands
public class ModCommandAttribute : PreconditionAttribute
{
    private readonly Config _config = FileHelper.LoadConfigAsync().Result;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
        CommandInfo command, IServiceProvider services) => Task.FromResult
            (
                context.User is not SocketGuildUser gUser ? PreconditionResult.FromError("You must be in a guild to run this command.") :
                gUser.Roles.Any(r => r.Id == _config.ModRole) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("")
            );
}
