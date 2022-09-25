using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.EventListeners;

public class UserListener
{
    // This listener handles listening to user related events (join, leave, etc)
    // This is mostly used for logging and constructing user settings
    
    private readonly LoggingService _logger;
    private readonly Dictionary<ulong, User> _users;
    private readonly ModLoggingService _modLogger;
    private readonly ModService _mod;
    private readonly ScheduleService _schedule;
    private readonly Config _config;
    
    public UserListener(LoggingService logger, Dictionary<ulong, User> users, ModLoggingService modLogger, ModService mod, ScheduleService schedule, Config config)
    {
        _logger = logger;
        _users = users;
        _modLogger = modLogger;
        _mod = mod;
        _schedule = schedule;
        _config = config;
    }

    public void RegisterEvents(DiscordSocketClient client)
    {
        client.UserJoined += (member) => Task.Factory.StartNew(async () => { await MemberJoinEvent(member); });
        client.UserLeft += (guild, user) => Task.Factory.StartNew(async () => { await MemberLeaveEvent(guild, user); });
    }

    private async Task MemberJoinEvent(SocketGuildUser member)
    {
        await _logger.Log($"New member join: {member.Username}#{member.DiscriminatorValue} ({member.Id})", level: LogLevel.Debug);
        if (!_users.ContainsKey(member.Id))
        {
            await _logger.Log($"No user data entry for new user, generating one now...", level: LogLevel.Debug);
            User newUser = new User();
            newUser.Username = $"{member.Username}#{member.Discriminator}";
            newUser.Aliases.Add(member.Username);
            newUser.Joins.Add(member.JoinedAt.Value); // I really fucking hope it isn't null the user literally just joined
            _users.Add(member.Id, newUser);
            await FileHelper.SaveUsersAsync(_users);
            await _logger.Log($"New user data entry generated.", level: LogLevel.Debug);
        }
        else
        {
            await _logger.Log($"Found user data entry for new user, add new join date", level: LogLevel.Debug);
            _users[member.Id].Joins.Add(member.JoinedAt.Value); // I still really fucking hope it isn't null because the user did just join
            await FileHelper.SaveUsersAsync(_users);
            await _logger.Log($"Added new join date for new user", level: LogLevel.Debug);
        }
        
        List<ulong> roles = new List<ulong>();
        string expiresString = "";

        await _logger.Log($"Processing roles for new user join", level: LogLevel.Debug);
        if (_config.GiveRolesOnJoin && _config.MemberRole != null && !(_config.AutoSilenceNewJoins || _users[member.Id].Silenced))
        {
            await _logger.Log($"Adding Config.MemberRole ({_config.MemberRole}) to new user", level: LogLevel.Debug);
            roles.Add((ulong)_config.MemberRole);
        }

        if (_config.GiveRolesOnJoin && _config.NewMemberRole != null && (!_config.AutoSilenceNewJoins || !_users[member.Id].Silenced))
        {
            await _logger.Log($"Adding Config.NewMemberRole ({_config.NewMemberRole}) to new user", level: LogLevel.Debug);
            roles.Add((ulong)_config.NewMemberRole);
            expiresString = $"{Environment.NewLine}New Member role expires in <t:{(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_config.NewMemberRoleDecay)).ToUnixTimeSeconds()}:R>";

            await _logger.Log($"Adding scheduled task to remove Config.NewMemberRole from new user in {_config.NewMemberRoleDecay} minutes", level: LogLevel.Debug);
            Dictionary<string, string> fields = new Dictionary<string, string>
            {
                { "roleId", _config.NewMemberRole.ToString() },
                { "userId", member.Id.ToString() },
                {
                    "reason",
                    $"{_config.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed."
                }
            };
            ScheduledTaskAction action = new ScheduledTaskAction(ScheduledTaskActionType.RemoveRole, fields);
            ScheduledTask task = new ScheduledTask(DateTimeOffset.UtcNow, 
                (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_config.NewMemberRoleDecay)), action);
            await _schedule.CreateScheduledTask(task, member.Guild);
            await _logger.Log($"Added scheduled task for new user", level: LogLevel.Debug);
        }
        
        await _logger.Log($"Generating action reason", level: LogLevel.Debug);
        
        string autoSilence = $" (User autosilenced, `AutoSilenceNewJoins` is true.)";
        
        if (roles.Count != 0)
        {
            if (!_config.AutoSilenceNewJoins) autoSilence = "";
            if (_users[member.Id].Silenced)
                autoSilence = 
                    ", silenced (attempted silence bypass)";
            await _logger.Log($"Generated action reason, executing action", level: LogLevel.Debug);

            await _mod.AddRoles(member, roles, DateTimeOffset.Now, 
                $"New user join{autoSilence}.{expiresString}"); 
            await _logger.Log($"Action executed, generating moderation log content", level: LogLevel.Debug);
        }

        autoSilence = ", silenced (`AutoSilenceNewJoins` is on)";
        if (!_config.AutoSilenceNewJoins) autoSilence = "";
        if (_users[member.Id].Silenced)
            autoSilence = 
                ", silenced (attempted silence bypass)";
        string joinedBefore = $", Joined {_users[member.Id].Joins.Count - 1} times before";
        if (_users[member.Id].Joins.Count <= 1) joinedBefore = "";
        await _logger.Log($"Generated moderation log content, posting log", level: LogLevel.Debug);
        
        await _modLogger.CreateModLog(member.Guild)
            .SetContent($"Join: <@{member.Id}> (`{member.Id}`), created <t:{member.CreatedAt.ToUnixTimeSeconds()}:R>{autoSilence}{joinedBefore}")
            .Send();
        await _logger.Log($"Log posted", level: LogLevel.Debug);
    }
    
    private async Task MemberLeaveEvent(SocketGuild guild, SocketUser user)
    {
        await _logger.Log($"Member leaving: {user.Username}#{user.Discriminator} ({user.Id}), getting last nickname", level: LogLevel.Debug);
        var lastNickname = "";
        try
        {
            lastNickname = _users[user.Id].Aliases.Last();
        }
        catch (InvalidOperationException)
        {
            lastNickname = "<UNKNOWN>";
        }
        await _logger.Log($"Last nickname was {lastNickname}, checking whether user was kicked or banned", level: LogLevel.Debug);
        var wasKicked = guild.GetAuditLogsAsync(2, actionType: ActionType.Kick).FirstAsync()
            .GetAwaiter().GetResult()
            .Select(audit =>
            {
                var data = audit.Data as KickAuditLogData;
                if (data.Target.Id == user.Id)
                {
                    if ((audit.CreatedAt.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 2)
                        return audit;
                }
                return null;
            });

        var wasBanned = guild.GetAuditLogsAsync(2, actionType: ActionType.Ban).FirstAsync()
            .GetAwaiter().GetResult()
            .Select(audit =>
            {
                var data = audit.Data as BanAuditLogData;
                if (data.Target.Id == user.Id)
                {
                    if ((audit.CreatedAt.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 2)
                        return audit;
                }
                return null;
            });

        await _logger.Log($"Constructing moderation log content", level: LogLevel.Debug);
        var output = 
            $"Leave: {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>";

        var banAuditLogEntries = wasBanned as RestAuditLogEntry[] ?? wasBanned.ToArray();
        if (banAuditLogEntries.Any(audit => audit != null))
        {
            await _logger.Log($"User was banned, fetching the reason and moderator", level: LogLevel.Debug);
            var audit = banAuditLogEntries.First();
            await _logger.Log($"Fetched, user was banned by {audit.User.Username}#{audit.User.Discriminator} ({audit.User.Id}) for \"{audit.Reason}\"", level: LogLevel.Debug);
            output =
                $"Leave (Ban): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
        }

        var kickAuditLogEntries = wasKicked as RestAuditLogEntry[] ?? wasKicked.ToArray();
        if (kickAuditLogEntries.Any(audit => audit != null))
        {
            await _logger.Log($"User was kicked, fetching the reason and moderator", level: LogLevel.Debug);
            var audit = kickAuditLogEntries.First();
            await _logger.Log($"Fetched, user was kicked by {audit.User.Username}#{audit.User.Discriminator} ({audit.User.Id}) for \"{audit.Reason}\"", level: LogLevel.Debug);
            output =
                $"Leave (Kick): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
        }
        await _logger.Log($"Finished constructing moderation log content", level: LogLevel.Debug);

        await _logger.Log($"Fetch all scheduled tasks for this user", level: LogLevel.Debug);
        var scheduledTasks = _schedule.GetScheduledTasks().ToList().Select(action =>
        {
            if (action.Action.Fields.ContainsKey("userId") &&
                action.Action.Fields["userId"] == user.Id.ToString())
                return action;
            return null;
        });

        await _logger.Log($"Cancelling all scheduled tasks for this user", level: LogLevel.Debug);
        foreach (var scheduledTask in scheduledTasks)
        {
            if (scheduledTask == null) continue;
            await _schedule.DeleteScheduledTask(scheduledTask);
        }
        await _logger.Log($"Cancelled all scheduled tasks for this user", level: LogLevel.Debug);

        await _logger.Log($"Sending moderation log", level: LogLevel.Debug);
        await _modLogger.CreateModLog(guild)
            .SetContent(output)
            .Send();
        await _logger.Log($"Moderation log sent", level: LogLevel.Debug);
    }
}