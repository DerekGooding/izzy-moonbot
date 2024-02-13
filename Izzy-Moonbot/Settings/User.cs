using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class User
{
    public User()
    {
        Username = "";
        Aliases = [];
        Joins = [];
        Silenced = false;
        RolesToReapplyOnRejoin = [];
        LastMessageTimeInMonitoredChannel = DateTimeOffset.MinValue;
    }

    public string Username { get; set; }
    public List<string> Aliases { get; set; }
    public List<DateTimeOffset> Joins { get; set; }
    public bool Silenced { get; set; }
    public HashSet<ulong> RolesToReapplyOnRejoin { get; set; }
    public DateTimeOffset LastMessageTimeInMonitoredChannel { get; set; }
}

public class PreviousMessageItem(ulong id, ulong channelId, ulong guildId, DateTimeOffset timestamp)
{
    public ulong Id { get; set; } = id;
    public ulong ChannelId { get; set; } = channelId;
    public ulong GuildId { get; set; } = guildId;
    public DateTimeOffset Timestamp { get; set; } = timestamp;
}
