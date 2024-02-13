using Izzy_Moonbot.Helpers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class RecentMessage(ulong messageId, ulong channelId, DateTimeOffset timestamp, string content, int embedsCount)
{
    public ulong MessageId = messageId;
    public ulong ChannelId = channelId;
    public DateTimeOffset Timestamp = timestamp;
    public string Content = content;
    public int EmbedsCount = embedsCount;

    public string GetJumpUrl() => $"https://discord.com/channels/{DiscordHelper.DefaultGuild()}/{ChannelId}/{MessageId}";
}

// Storage for Izzy's transient shared state.
// This is used for volatile data that needs to be used by multiple services and modules.
public class TransientState
{
    public int CurrentLargeJoinCount = 0;
    public int CurrentSmallJoinCount = 0;

    public DateTimeOffset LastWittyResponse = DateTimeOffset.MinValue;

    // AdminModule
    public DateTimeOffset LastMentionResponse = DateTimeOffset.MinValue;

    // RaidService
    public List<ulong> RecentJoins = [];

    public ConcurrentDictionary<ulong, ConcurrentQueue<RecentMessage>> RecentMessages = new();
}
