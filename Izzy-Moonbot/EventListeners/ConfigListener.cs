using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Types;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Izzy_Moonbot.EventListeners;

public class ConfigListener(Config config, LoggingService logger, ScheduleService schedule)
{
    private readonly Config _config = config;
    private readonly LoggingService _logger = logger;

    private readonly ScheduleService _schedule = schedule;

    public void RegisterEvents(DiscordSocketClient client)
    {
        _config.Changed += (thing, e) => Task.Run(async () => { await ConfigChangeEvent(e, client); });
    }

    public async Task ConfigChangeEvent(ConfigValueChangeEvent e, DiscordSocketClient client)
    {
        _logger.Log($"Config value change: {e.Name} from {e.Original} to {e.Current}", level: LogLevel.Debug);

        switch (e.Name)
        {
            case "BannerMode":
                await Handle_BannerMode(e, client);
                break;

            case "BannerInterval":
                await Handle_BannerInterval(e);
                break;

            case "BoredChannel":
                await Handle_BoredChannel(e, client);
                break;

            case "BoredCooldown":
                await Handle_BoredCooldown(e);
                break;

            default:
                throw new NotImplementedException("This config value doesn't have a method to fire on change.");
        }
    }

    private async Task Handle_BannerMode(ConfigValueChangeEvent e, DiscordSocketClient client)
    {
        /*
         * If BannerMode is `None`, Izzy deletes the internal repeating task.
         * Else, she'll create it if it doesn't exist, or leave it be.
         */
        var original = e.Original is BannerMode originalMode ? originalMode : BannerMode.None;
        var current = e.Current is BannerMode currentMode ? currentMode : BannerMode.None;

        if (original == BannerMode.None && current != BannerMode.None)
        {
            if (_config.BannerInterval <= 0)
            {
                _logger.Log($"For some reason BannerInterval is non-positive, so we can't create a banner rotation task.");
                return;
            }

            // Create repeating job.
            var currentTime = DateTimeOffset.UtcNow;
            var executeTime = currentTime.AddMinutes(_config.BannerInterval);

            _logger.Log($"Adding scheduled job to run the banner rotation job in {_config.BannerInterval} minutes", level: LogLevel.Debug);
            Dictionary<string, string> fields = [];
            var action = new ScheduledBannerRotationJob();
            var job = new ScheduledJob(currentTime, executeTime, action, ScheduledJobRepeatType.Relative);
            await _schedule.CreateScheduledJob(job);
            _logger.Log($"Added scheduled job {job.Id} for banner rotation.");
        }
        else if (original != BannerMode.None && current == BannerMode.None)
        {
            // Delete repeated job.
            var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBannerRotationJob);

            _logger.Log($"Cancelling all scheduled jobs for banner rotation.", level: LogLevel.Debug);
            foreach (var scheduledJob in scheduledJobs)
            {
                await _schedule.DeleteScheduledJob(scheduledJob);
            }
        }

        // If we're managing the banner, make sure the banner is immediately updated to match the new mode
        if (current != BannerMode.None)
        {
            var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBannerRotationJob);

            foreach (var scheduledJob in scheduledJobs)
            {
                _logger.Log($"Immediately invoking banner rotation job {scheduledJob.Id} due to config change.");
                await _schedule.Unicycle_BannerRotation((ScheduledBannerRotationJob)scheduledJob.Action, new SocketGuildAdapter(client.GetGuild(DiscordHelper.DefaultGuild())), new DiscordSocketClientAdapter(client));
            }
        }
    }

    private async Task Handle_BannerInterval(ConfigValueChangeEvent e)
    {
        if (e.Original == e.Current) return;

        var original = e.Original is double originalDouble ? originalDouble : 0;
        var current = e.Current is double currentDouble ? currentDouble : 0;

        var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBannerRotationJob);

        _logger.Log($"Updating all scheduled jobs for banner rotation to occur {current} minutes after enabling rotation instead of after {original} minutes.");
        foreach (var scheduledJob in scheduledJobs)
        {
            scheduledJob.ExecuteAt = (scheduledJob.LastExecutedAt ?? scheduledJob.CreatedAt).AddMinutes(current);
            await _schedule.ModifyScheduledJob(scheduledJob.Id, scheduledJob);
        }
    }

    public enum BannerMode
    {
        None,
        Rotate,
        Shuffle,
        ManebooruFeatured
    }

    private async Task Handle_BoredChannel(ConfigValueChangeEvent e, DiscordSocketClient _)
    {
        var original = e.Original is ulong originalValue ? originalValue : 0;
        var current = e.Current is ulong currentValue ? currentValue : 0;

        if (original == current) return;

        _logger.Log($"Clearing and recreating scheduled job for bored commands because bored channel changed from {original} to {current}.");
        var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBoredCommandsJob);

        if (current == 0 && scheduledJobs.Count != 0)
        {
            foreach (var scheduledJob in scheduledJobs)
                await _schedule.DeleteScheduledJob(scheduledJob);
        }
        else if (current != 0 && scheduledJobs.Count == 0)
        {
            var nextJob = new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow, new ScheduledBoredCommandsJob(), ScheduledJobRepeatType.None);
            await _schedule.CreateScheduledJob(nextJob);
        }
    }

    private async Task Handle_BoredCooldown(ConfigValueChangeEvent e)
    {
        var original = e.Original is double originalValue ? originalValue : 0;
        var current = e.Current is double currentValue ? currentValue : 0;

        if (original == current) return;

        var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBoredCommandsJob);

        if (scheduledJobs.Count == 0)
        {
            _logger.Log($"BoredCooldown changed, but no bored job exists. Creating a bored commands job.");
            var nextJob = new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow, new ScheduledBoredCommandsJob(), ScheduledJobRepeatType.None);
            await _schedule.CreateScheduledJob(nextJob);
            scheduledJobs.Add(nextJob);
        }

        foreach (var scheduledJob in scheduledJobs)
        {
            _logger.Log($"Updating bored commands job(s) to execute after {current} seconds instead of {original} seconds.");
            scheduledJob.ExecuteAt = scheduledJob.CreatedAt.AddSeconds(current);
            await _schedule.ModifyScheduledJob(scheduledJob.Id, scheduledJob);
        }
    }
}
