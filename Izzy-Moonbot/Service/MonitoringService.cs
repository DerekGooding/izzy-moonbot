using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Service;

public class MonitoringService(Config config, Dictionary<ulong, User> users)
{
    private readonly Config _config = config;
    private readonly Dictionary<ulong, User> _users = users;

    public void RegisterEvents(IIzzyClient client)
        => client.MessageReceived += async (message) => await DiscordHelper.LeakOrAwaitTask(ProcessMessage(message, client));

    private async Task ProcessMessage(IIzzyMessage message, IIzzyClient client)
    {
        if (!_config.MonitoringEnabled) return;
        if (message.Author.Id == client.CurrentUser.Id) return; // Don't process self
        if (message.Author.IsBot) return; // Don't process bots
        if (!DiscordHelper.IsInGuild(message)) return; // Don't process DMs
        if (!DiscordHelper.IsProcessableMessage(message)) return; // Not processable
        if (message is not IIzzyUserMessage userMessage) return; // Not processable

        IIzzyContext context = client.MakeContext(userMessage);

        if (!DiscordHelper.IsDefaultGuild(context)) return;

        await ProcessLimitTrip(context);
    }

    private async Task ProcessLimitTrip(IIzzyContext context)
    {
        if (context.User is not IIzzyGuildUser guildUser) return; // Not processable
        if (_config.MonitoringChannel != context.Channel.Id) return; // Message is in the right channel
        if (_config.MonitoringBypassRoles.Overlaps(guildUser.Roles.Select(role => role.Id))) return; // User doesn't have bypass role

        User user = _users[guildUser.Id];
        if (user == null) return;

        TimeSpan monitoringPeriod = TimeSpan.FromMinutes(_config.MonitoringMessageInterval);

        if (context.Message.CreatedAt - user.LastMessageTimeInMonitoredChannel > monitoringPeriod)
        {
            // user has waited long enough, allow
            user.LastMessageTimeInMonitoredChannel = context.Message.CreatedAt;
            await FileHelper.SaveUsersAsync(_users);
        }
        else
        {
            // too soon, smack them
            await context.Message.DeleteAsync();
            await context.Message.Channel.SendMessageAsync($"<@{guildUser.Id}> sorry that I had to remove your post, but it hasn't been {GetReadableTimeSpanString(monitoringPeriod)} since your last one yet!");
        }
    }

    private static string GetReadableTimeSpanString(TimeSpan timeSpan)
    {
        List<string> timeStringParts = [];

        if (timeSpan.Days > 0) timeStringParts.Add(PluralizeSimple(timeSpan.Days, "day"));
        if (timeSpan.Hours > 0) timeStringParts.Add(PluralizeSimple(timeSpan.Hours, "hour"));
        if (timeSpan.Minutes > 0) timeStringParts.Add(PluralizeSimple(timeSpan.Minutes, "minute"));
        if (timeSpan.Seconds > 0) timeStringParts.Add(PluralizeSimple(timeSpan.Seconds, "second"));

        return string.Join(" ", timeStringParts);
    }

    private static string PluralizeSimple(int amount, string noun) => $"{amount} {noun}" + (amount == 1 ? "" : "s");
}
