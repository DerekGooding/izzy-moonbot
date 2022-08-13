﻿using System.Linq;
using System.Net;
using Discord;

namespace Izzy_Moonbot.Service
{
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ModService
    {
        private ServerSettings _settings;
        private Dictionary<ulong, User> _users;
        private ModLoggingService _modLog;

        public ModService(ServerSettings settings, Dictionary<ulong, User> users, ModLoggingService modLog)
        {
            _settings = settings;
            _users = users;
            _modLog = modLog;
        }

        /*public async Task<string> GenerateSuggestedLog(ActionType action, SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason)
        {
            string actionName = GetActionName(action);
            string untilTimestamp = "";

            if (until.HasValue == false) untilTimestamp = "Never (Permanent)";
            else
            {
                long untilUnixTimestamp = until.Value.ToUnixTimeSeconds();
                untilTimestamp = $"<t:{untilUnixTimestamp}:F>";
            }

            string template = $"Type: {actionName}{Environment.NewLine}" +
                   $"User: {target.Mention} (`{target.Id}`){Environment.NewLine}" +
                   $"Expires: {untilTimestamp}{Environment.NewLine}" +
                   $"Info: {reason}";

            return template;
        }*/

        public async Task SilenceUser(SocketGuildUser user, DateTimeOffset time, DateTimeOffset? until, string reason = "No reason provided.")
        {
            if (user.IsBot) throw new NotSupportedException("Bots cannot be silenced.");
            if (!(_settings.SafeMode))
            {
                //await target.RemoveRoleAsync(_settings.MemberRole);

                _users[user.Id].Silenced = true;
                await FileHelper.SaveUsersAsync(_users);
            }
            
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.Silence)
                .AddTarget(user)
                .SetTime(time)
                .SetUntilTime(until)
                .SetReason(reason)
                .Send();
        }
        
        public async Task SilenceUsers(List<SocketGuildUser> users, DateTimeOffset time, DateTimeOffset? until, string reason = "No reason provided.")
        {
            if (users.Count == 0) throw new NullReferenceException("users must have users in them");
            var actionLog = _modLog.CreateActionLog(users[0].Guild)
                .SetActionType(LogType.Silence)
                .SetTime(time)
                .SetUntilTime(until)
                .SetReason(reason);

            foreach (var user in users)
            {
                if (user.IsBot) throw new NotSupportedException("Bots cannot be silenced.");

                actionLog.AddTarget(user);
                
                if (_settings.SafeMode) continue;
                //await target.RemoveRoleAsync(_settings.MemberRole);

                _users[user.Id].Silenced = true;
                await FileHelper.SaveUsersAsync(_users);
            }
            
            await actionLog.Send();
        }

        public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason = null, bool log = true)
        {
            //if (!_settings.SafeMode) await user.AddRoleAsync(roleId);

            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.AddRoles)
                .AddTarget(user)
                .AddRole(roleId)
                .SetReason(reason)
                .Send();
        }
        
        public async Task AddRoleToUsers(List<SocketGuildUser> users, ulong roleId, string? reason = null, bool log = true)
        {
            var actionLog = _modLog.CreateActionLog(users[0].Guild)
                .SetActionType(LogType.AddRoles)
                .AddRole(roleId)
                .SetReason(reason);
            
            foreach (var socketGuildUser in users)
            {
                //if (!_settings.SafeMode) await user.AddRoleAsync(roleId);
                actionLog.AddTarget(socketGuildUser);
            }

            await actionLog.Send();
        }
        
        public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason = null, bool log = true)
        {
            //if (!_settings.SafeMode) await user.RemoveRoleAsync(roleId);
                
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.RemoveRoles)
                .AddTarget(user)
                .AddRole(roleId)
                .SetReason(reason)
                .Send();
        }
        
        public async Task RemoveRoleFromUsers(List<SocketGuildUser> users, ulong roleId, string? reason = null, bool log = true)
        {
            var actionLog = _modLog.CreateActionLog(users[0].Guild)
                .SetActionType(LogType.RemoveRoles)
                .AddRole(roleId)
                .SetReason(reason);
            
            foreach (var socketGuildUser in users)
            {
                //if (!_settings.SafeMode) await user.RemoveRoleAsync(roleId);
                actionLog.AddTarget(socketGuildUser);
            }

            await actionLog.Send();
        }
        
        public async Task AddRoles(SocketGuildUser user, List<ulong> roles, string? reason = null)
        {
            //if (!_settings.SafeMode) await user.AddRolesAsync(roles);
            
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.AddRoles)
                .AddTarget(user)
                .AddRoles(roles)
                .SetReason(reason)
                .Send();
        }
        
        public async Task AddRolesToUsers(List<SocketGuildUser> users, List<ulong> roles, string? reason = null)
        {
            var actionLog = _modLog.CreateActionLog(users[0].Guild)
                .SetActionType(LogType.AddRoles)
                .AddRoles(roles)
                .SetReason(reason);
            
            foreach (var socketGuildUser in users)
            {
                //if (!_settings.SafeMode) await user.AddRolesAsync(roleId);
                actionLog.AddTarget(socketGuildUser);
            }

            await actionLog.Send();
        }
        
        public async Task RemoveRoles(SocketGuildUser user, List<ulong> roles, string? reason = null)
        {
            //if (!_settings.SafeMode) await user.RemoveRolesAsync(roles);
            
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.RemoveRoles)
                .AddTarget(user)
                .AddRoles(roles)
                .SetReason(reason)
                .Send();
        }
        
        public async Task RemoveRolesFromUsers(List<SocketGuildUser> users, List<ulong> roles, string? reason = null)
        {
            var actionLog = _modLog.CreateActionLog(users[0].Guild)
                .SetActionType(LogType.RemoveRoles)
                .AddRoles(roles)
                .SetReason(reason);
            
            foreach (var socketGuildUser in users)
            {
                //if (!_settings.SafeMode) await user.RemoveRolesAsync(roleId);
                actionLog.AddTarget(socketGuildUser);
            }

            await actionLog.Send();
        }
    }
}
