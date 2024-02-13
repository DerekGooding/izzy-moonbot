using Discord.Commands;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Attributes;

// Allow users to use commands outside of DiscordSettings.DefaultGuild.
public class ExternalUsageAllowed : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
        CommandInfo command, IServiceProvider services) => Task.FromResult(PreconditionResult.FromSuccess());
}
