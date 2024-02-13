using Discord;
using Izzy_Moonbot.Adapters;

namespace Izzy_Moonbot.Settings;

public class ScheduledJob(DateTimeOffset createdAt, DateTimeOffset executeAt, ScheduledJobAction action,
    ScheduledJobRepeatType repeatType = ScheduledJobRepeatType.None)
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset CreatedAt { get; set; } = createdAt;
    public DateTimeOffset? LastExecutedAt { get; set; } = null;
    public DateTimeOffset ExecuteAt { get; set; } = executeAt;
    public ScheduledJobAction Action { get; set; } = action;
    public ScheduledJobRepeatType RepeatType { get; set; } = repeatType;

    public string ToDiscordString()
        => $"`{Id}`: {Action.ToDiscordString()} <t:{ExecuteAt.ToUnixTimeSeconds()}:R>{(RepeatType != ScheduledJobRepeatType.None ?
           $", repeating {RepeatType}{(LastExecutedAt != null ? $", last executed <t:{LastExecutedAt.Value.ToUnixTimeSeconds()}:R>" : "")}" : "")}.";

    public string ToFileString()
        => $"{Id}: {Action.ToFileString()} at {ExecuteAt:F}{(RepeatType == ScheduledJobRepeatType.None ?
           "" : $", repeating {RepeatType}{(LastExecutedAt != null ? $", last executed at {LastExecutedAt.Value:F}" : "")}")}.";
}

// Class only exists to be extended so we can have a single ScheduledJob class.
public class ScheduledJobAction
{
    public ScheduledJobActionType Type { get; protected set; }

    public virtual string ToDiscordString() => "Unknown Scheduled Job Action";

    public virtual string ToFileString() => ToDiscordString();
}

/* Scheduled Job Action types */

public class ScheduledRoleJob : ScheduledJobAction
{
    public ulong Role { get; protected set; }
    public ulong User { get; protected set; }
    public string? Reason { get; protected set; }
}

public class ScheduledRoleRemovalJob : ScheduledRoleJob
{
    public ScheduledRoleRemovalJob(ulong role, ulong user, string? reason)
    {
        Type = ScheduledJobActionType.RemoveRole;

        Role = role;
        User = user;
        Reason = reason;
    }

    public ScheduledRoleRemovalJob(IRole role, IGuildUser user, string? reason)
    {
        Type = ScheduledJobActionType.RemoveRole;

        Role = role.Id;
        User = user.Id;
        Reason = reason;
    }

    public override string ToDiscordString() => $"Remove <@&{Role}> (`{Role}`) from <@{User}> (`{User}`)";

    public override string ToFileString() => $"Remove role {Role} from user {User}";
}

public class ScheduledRoleAdditionJob : ScheduledRoleJob
{
    public ScheduledRoleAdditionJob(ulong role, ulong user, string? reason)
    {
        Type = ScheduledJobActionType.AddRole;

        Role = role;
        User = user;
        Reason = reason;
    }

    public ScheduledRoleAdditionJob(IIzzyRole role, IIzzyGuildUser user, string? reason)
    {
        Type = ScheduledJobActionType.AddRole;

        Role = role.Id;
        User = user.Id;
        Reason = reason;
    }

    public override string ToDiscordString() => $"Add <@&{Role}> (`{Role}`) to <@{User}> (`{User}`)";

    public override string ToFileString() => $"Add role {Role} to user {User}";
}

public class ScheduledUnbanJob : ScheduledJobAction
{
    public ScheduledUnbanJob(ulong user, string? reason)
    {
        Type = ScheduledJobActionType.Unban;

        User = user;
        Reason = reason;
    }

    public ScheduledUnbanJob(IIzzyUser user, string? reason)
    {
        Type = ScheduledJobActionType.Unban;

        User = user.Id;
        Reason = reason;
    }

    public ulong User { get; }
    public string? Reason { get; }

    public override string ToDiscordString() => $"Unban <@{User}> (`{User}`)";

    public override string ToFileString() => $"Unban user {User}";
}

public class ScheduledEchoJob : ScheduledJobAction
{
    public ScheduledEchoJob(ulong channelOrUser, string content)
    {
        Type = ScheduledJobActionType.Echo;

        ChannelOrUser = channelOrUser;
        Content = content;
    }

    public ScheduledEchoJob(IIzzyMessageChannel channel, string content)
    {
        Type = ScheduledJobActionType.Echo;

        ChannelOrUser = channel.Id;
        Content = content;
    }

    public ScheduledEchoJob(IIzzyUser user, string content)
    {
        Type = ScheduledJobActionType.Echo;

        ChannelOrUser = user.Id;
        Content = content;
    }

    public ulong ChannelOrUser { get; }
    public string Content { get; }

    public override string ToDiscordString() => $"Send \"{Content}\" to (<#{ChannelOrUser}>/<@{ChannelOrUser}>) (`{ChannelOrUser}`)";

    public override string ToFileString() => $"Send \"{Content}\" to channel/user {ChannelOrUser}";
}

public class ScheduledBannerRotationJob : ScheduledJobAction
{
    public ScheduledBannerRotationJob(int? lastBannerIndex = null)
    {
        Type = ScheduledJobActionType.BannerRotation;

        LastBannerIndex = lastBannerIndex;
    }

    // Only used in Rotate mode
    public int? LastBannerIndex { get; set; }

    public override string ToDiscordString() => $"Run Banner Rotation";

    public override string ToFileString() => ToDiscordString();
}

public class ScheduledBoredCommandsJob : ScheduledJobAction
{
    public ScheduledBoredCommandsJob() =>
        Type = ScheduledJobActionType.BoredCommands;

    public override string ToDiscordString() => $"Run Bored Commands";

    public override string ToFileString() => ToDiscordString();
}

public class ScheduledEndRaidJob : ScheduledJobAction
{
    public ScheduledEndRaidJob(bool isLarge)
    {
        Type = ScheduledJobActionType.EndRaid;
        IsLarge = isLarge;
    }

    public bool IsLarge { get; }

    public override string ToDiscordString() => $"End {(IsLarge ? "large" : "small")} raid";

    public override string ToFileString() => ToDiscordString();
}

public enum ScheduledJobActionType
{
    RemoveRole,
    AddRole,
    Unban,
    Echo,
    BannerRotation,
    BoredCommands,
    EndRaid
}

public enum ScheduledJobRepeatType
{
    None,
    Relative,
    Daily,
    Weekly,
    Yearly
}
