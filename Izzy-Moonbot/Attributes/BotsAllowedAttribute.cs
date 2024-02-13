using Discord.Commands;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Attributes;

// Allow bots to use commands.
// This is so we can say "bots can use the roll command but not the ban command"
public class BotsAllowedAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
        CommandInfo command, IServiceProvider services) => Task.FromResult(PreconditionResult.FromSuccess());
}
