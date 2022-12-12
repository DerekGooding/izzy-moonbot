using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Flurl.Http;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;
using static Izzy_Moonbot.Settings.ScheduledJobRepeatType;

namespace Izzy_Moonbot.Service;

/// <summary>
/// Service responsible for the management and execution of scheduled tasks which need to be non-volatile.
/// </summary>
public class ScheduleService
{
    private readonly Config _config;
    private readonly LoggingService _logger;
    private readonly ModService _mod;
    private readonly ModLoggingService _modLogging;
    private GeneralStorage _generalStorage;

    private readonly List<ScheduledJob> _scheduledJobs;

    private bool _alreadyInitiated;

    public ScheduleService(Config config, ModService mod, ModLoggingService modLogging, LoggingService logger, GeneralStorage generalStorage,
        List<ScheduledJob> scheduledJobs)
    {
        _config = config;
        _logger = logger;
        _mod = mod;
        _modLogging = modLogging;
        _generalStorage = generalStorage;
        _scheduledJobs = scheduledJobs;
    }

    public void BeginUnicycleLoop(DiscordSocketClient client)
    {
        if (_alreadyInitiated) return;
        _alreadyInitiated = true;
        UnicycleLoop(client);
    }

    private void UnicycleLoop(DiscordSocketClient client)
    {
        // Core event loop. Executes every Config.UnicycleInterval seconds.
        Task.Run(async () =>
        {
            await Task.Delay(_config.UnicycleInterval);
            
            // Run unicycle.
            try
            {
                await Unicycle(client);
            }
            catch (Exception exception)
            {
                await _logger.Log($"{exception.Message}{Environment.NewLine}{exception.StackTrace}", level: LogLevel.Error);
            }

            // Call self
            UnicycleLoop(client);
        });
    }

    private async Task Unicycle(DiscordSocketClient client)
    {
        var scheduledJobsToExecute = new List<ScheduledJob>();

        foreach (var job in _scheduledJobs)
        {
            if (job.ExecuteAt.ToUnixTimeMilliseconds() <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                scheduledJobsToExecute.Add(job);
            }
        }

        foreach (var job in scheduledJobsToExecute)
        {
            await _logger.Log($"Executing scheduled job queued for execution at {job.ExecuteAt:F}", level: LogLevel.Debug);

            try
            {
                // Do processing here I guess!
                bool completed;
                
                switch (job.Action.GetType().Name)
                {
                    case "ScheduledRoleRemovalJob":
                        completed = await Unicycle_RemoveRole((ScheduledRoleRemovalJob)job.Action,
                            client.GetGuild(DiscordHelper.DefaultGuild()));
                        break;
                    case "ScheduledRoleAdditionJob":
                        completed = await Unicycle_AddRole((ScheduledRoleAdditionJob)job.Action,
                            client.GetGuild(DiscordHelper.DefaultGuild()));
                        break;
                    case "ScheduledUnbanJob":
                        completed = await Unicycle_Unban((ScheduledUnbanJob)job.Action,
                            client.GetGuild(DiscordHelper.DefaultGuild()), client);
                        break;
                    case "ScheduledEchoJob":
                        completed = await Unicycle_Echo((ScheduledEchoJob)job.Action,
                            client.GetGuild(DiscordHelper.DefaultGuild()), client);
                        break;
                    case "ScheduledBannerRotationJob":
                        completed = await Unicycle_BannerRotation((ScheduledBannerRotationJob)job.Action,
                            client.GetGuild(DiscordHelper.DefaultGuild()), client);
                        break;
                    default:
                        throw new NotSupportedException($"{job.Action.GetType().Name} is currently not supported.");
                }

                if (!completed)
                {
                    await _logger.Log(
                        $"Scheduled job did not complete successfully but didn't throw an error. It likely received invalid data.{Environment.NewLine}" +
                        $"Job: {job}",
                        level: LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                await _logger.Log(
                    $"Scheduled job threw an exception when trying to execute!{Environment.NewLine}" +
                    $"Type: {ex.GetType().Name}{Environment.NewLine}" +
                    $"Message: {ex.Message}{Environment.NewLine}" +
                    $"Job: {job}{Environment.NewLine}" +
                    $"Stack Trace: {ex.StackTrace}");
            }

            await DeleteOrRepeatScheduledJob(job);
        }
    }

    public ScheduledJob GetScheduledJob(string id)
    {
        return _scheduledJobs.Single(job => job.Id == id);
    }

    public ScheduledJob GetScheduledJob(Func<ScheduledJob, bool> predicate)
    {
        return _scheduledJobs.Single(predicate);
    }
    
    public List<ScheduledJob> GetScheduledJobs()
    {
        return _scheduledJobs.ToList();
    }

    public List<ScheduledJob> GetScheduledJobs(Func<ScheduledJob, bool> predicate)
    {
        return _scheduledJobs.Where(predicate).ToList();
    }

    public async Task CreateScheduledJob(ScheduledJob job)
    {
        _scheduledJobs.Add(job);
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    public async Task ModifyScheduledJob(string id, ScheduledJob job)
    {
        _scheduledJobs[_scheduledJobs.IndexOf(_scheduledJobs.First(altJob => altJob.Id == id))] = job;
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    public async Task DeleteScheduledJob(ScheduledJob job)
    {
        var result = _scheduledJobs.Remove(job);
        if (!result)
            throw new NullReferenceException("The scheduled job provided was not found in the scheduled job list.");
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    private async Task DeleteOrRepeatScheduledJob(ScheduledJob job)
    {
        if (job.RepeatType != None)
        {
            // Modify job to allow repeatability.
            var taskIndex = _scheduledJobs.FindIndex(scheduledJob => scheduledJob.Id == job.Id);
            
            // Get LastExecutedAt, or CreatedAt if former is null as well as the execution time.
            var creationAt = job.LastExecutedAt ?? job.CreatedAt;
            var executeAt = job.ExecuteAt;

            // RepeatType is checked against null above.
            switch (job.RepeatType)
            {
                case Relative:
                    // Get the offset.
                    var repeatEvery = executeAt - creationAt;
            
                    // Get the timestamp of next execution.
                    var nextExecuteAt = executeAt + repeatEvery;
            
                    // Set previous execution time and new execution time
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = nextExecuteAt;
                    break;
                case Daily:
                    // Just add a single day to the execute at time lol
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = executeAt.AddDays(1);
                    break;
                case Weekly:
                    // Add 7 days to the execute at time
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = executeAt.AddDays(7);
                    break;
                case Yearly:
                    // Add a year to the execute at time
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = executeAt.AddYears(1);
                    break;
            }

            // Update the task and save
            _scheduledJobs[taskIndex] = job;
            await FileHelper.SaveScheduleAsync(_scheduledJobs);

            return;
        }

        await DeleteScheduledJob(job);
    }
    
    // Executors for different types.
    private async Task<bool> Unicycle_AddRole(ScheduledRoleAdditionJob job, SocketGuild guild)
    {
        var role = guild.GetRole(job.Role);
        var user = guild.GetUser(job.User);
        if (role == null || user == null) return false;

        var reason = job.Reason;
        
        await _logger.Log(
            $"Adding {role.Name} ({role.Id}) to {user.Username}#{user.Discriminator} ({user.Id})", level: LogLevel.Debug);
        
        await _mod.AddRole(user, role.Id, reason);
        await _modLogging.CreateModLog(guild)
            .SetContent(
                $"Gave <@&{role.Id}> to <@{user.Id}> (`{user.Id}`).")
            .SetFileLogContent(
                $"Gave {role.Name} ({role.Id}) to {user.Username}#{user.Discriminator} ({user.Id}). {(reason != null ? $"Reason: {reason}." : "")}")
            .Send();

        return true;
    }
    
    private async Task<bool> Unicycle_RemoveRole(ScheduledRoleRemovalJob job, SocketGuild guild)
    {
        var role = guild.GetRole(job.Role);
        var user = guild.GetUser(job.User);
        if (role == null || user == null) return false;

        string? reason = job.Reason;
        
        await _logger.Log(
            $"Removing {role.Name} ({role.Id}) from {user.Username}#{user.Discriminator} ({user.Id})", level: LogLevel.Debug);
        
        await _mod.RemoveRole(user, role.Id, reason);
        await _modLogging.CreateModLog(guild)
            .SetContent(
                $"Removed <@&{role.Id}> from <@{user.Id}> (`{user.Id}`)")
            .SetFileLogContent(
                $"Removed {role.Name} ({role.Id}) from {user.Username}#{user.Discriminator} ({user.Id}). {(reason != null ? $"Reason: {reason}." : "")}")
            .Send();

        return true;
    }

    private async Task<bool> Unicycle_Unban(ScheduledUnbanJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (await guild.GetBanAsync(job.User) == null) return false;

        var user = await client.GetUserAsync(job.User);
        
        await _logger.Log(
            $"Unbanning {(user == null ? job.User : $"")}.",
            level: LogLevel.Debug);

        await guild.RemoveBanAsync(job.User);

        var embed = new EmbedBuilder()
            .WithTitle(
                $"Unbanned {(user != null ? $"{user.Username}#{user.Discriminator} " : "")}<@{job.User}> ({job.User})")
            .WithColor(16737792)
            .WithFooter("Gasp! Does this mean I can invite them to our next traditional unicorn sleepover?")
            .Build();
        
        await _modLogging.CreateModLog(guild)
            .SetEmbed(embed)
            .SetFileLogContent($"Unbanned {(user != null ? $"{user.Username}#{user.Discriminator} " : "")} ({job.User})")
            .Send();

        return true;
    }

    private async Task<bool> Unicycle_Echo(ScheduledEchoJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (job.Content == "") return false;
        var channel = guild.GetTextChannel(job.Channel);
        if (channel == null)
        {
            var user = await client.GetUserAsync(job.Channel);
            if (user == null) return false;

            await user.SendMessageAsync(job.Content);
            return true;
        }

        await channel.SendMessageAsync(job.Content);
        return true;
    }

    public async Task<bool> Unicycle_BannerRotation(ScheduledBannerRotationJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (_config.BannerMode == ConfigListener.BannerMode.None) return false;
        if (_config.BannerMode == ConfigListener.BannerMode.CustomRotation && _config.BannerImages.Count == 0) return false;

        if (_config.BannerMode == ConfigListener.BannerMode.CustomRotation)
        {
            try
            {
                // Rotate through banners.
                var rand = new Random();
                var number = rand.Next(_config.BannerImages.Count);
                var url = _config.BannerImages.ToList()[number];
                Stream stream;
                try
                {
                    stream = await url
                        .WithHeader("user-agent", $"Izzy-Moonbot (Linux x86_64) Flurl.Http/3.2.4 DotNET/6.0")
                        .GetStreamAsync();
                }
                catch (FlurlHttpException ex)
                {
                    await _logger.Log($"Recieved HTTP exception when executing Banner Rotation: {ex.Message}");
                    return false;
                }

                var image = new Image(stream);

                await guild.ModifyAsync(properties => properties.Banner = image);

                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Changed banner to <{url}> for banner rotation.")
                    .SetFileLogContent(
                        $"Changed banner to {url} for banner rotation.")
                    .Send();
            }
            catch (FlurlHttpTimeoutException ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner but the host server didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .SetFileLogContent(
                        $"Tried to change banner but the host server didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP timeout exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (FlurlHttpException ex)
            {
                // Http request failure.
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a {ex.StatusCode} status code when attempting to ask the host server for the image. Doing nothing.")
                    .SetFileLogContent(
                        $"Tried to change banner and received a {ex.StatusCode} status code when attempting to ask the host server for the image. Doing nothing.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a general error when attempting to ask the host server for the image. Doing nothing.")
                    .SetFileLogContent(
                        $"Tried to change banner and received a general error when attempting to ask the host server for the image. Doing nothing.")
                    .Send();
                await _logger.Log(
                    $"Encountered exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
        }
        else if (_config.BannerMode == ConfigListener.BannerMode.ManebooruFeatured)
        {
            // Set to Manebooru featured.
            try
            {
                var image = await BooruHelper.GetFeaturedImage();

                if (_generalStorage.CurrentBooruFeaturedImage != null)
                {
                    if (image.Id == _generalStorage.CurrentBooruFeaturedImage.Id)
                    {
                        // Update the cache in case of change, but return
                        _generalStorage.CurrentBooruFeaturedImage =
                            image;
                        await FileHelper.SaveGeneralStorageAsync(_generalStorage);
                        return true;
                    }
                }

                // Don't check the images if they're not ready yet!
                if (!image.ThumbnailsGenerated || image.Representations == null)
                {
                    await _modLogging.CreateModLog(guild)
                        .SetContent(
                            $"Tried to change banner to <https://manebooru.art/images/{image.Id}> but that image hasn't fully been generated yet. Doing nothing and trying again in {_config.BannerInterval} minutes.")
                        .SetFileLogContent(
                            $"Tried to change banner to https://manebooru.art/images/{image.Id} but that image hasn't fully been generated yet. Doing nothing and trying again in {_config.BannerInterval} minutes.")
                        .Send();
                    return true;
                }

                if (image.Spoilered)
                {
                    // Image is blocked by current filter, complain.
                    await _modLogging.CreateModLog(guild)
                        .SetContent(
                            $"Tried to change banner to <https://manebooru.art/images/{image.Id}> but that image is blocked by my filter! Doing nothing.")
                        .SetFileLogContent(
                            $"Tried to change banner to https://manebooru.art/images/{image.Id} but that image is blocked by my filter! Doing nothing.")
                        .Send();
                    return true;
                }

                var imageStream = await image.Representations.Thumbnail.GetStreamAsync();

                await guild.ModifyAsync(properties => properties.Banner = new Image(imageStream));
                
                _generalStorage.CurrentBooruFeaturedImage =
                    image;
                await FileHelper.SaveGeneralStorageAsync(_generalStorage);
                
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Changed banner to <https://manebooru.art/images/{image.Id}> for Manebooru featured image.")
                    .SetFileLogContent(
                        $"Changed banner to https://manebooru.art/images/{image.Id} for Manebooru featured image.")
                    .Send();
            }
            catch (FlurlHttpTimeoutException ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner but Manebooru didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .SetFileLogContent(
                        $"Tried to change banner but Manebooru didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP timeout exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (FlurlHttpException ex)
            {
                // Http request failure.
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a {ex.StatusCode} status code when attempting to ask Manebooru for the featured image. Doing nothing.\n" +
                        $"Likely causes:\n" +
                        $"  - I sent a badly formatted request to Manebooru.\n" +
                        $"  - Manebooru thinks I sent a badly formatted request when I didn't.\n" +
                        $"  - Manebooru is down and Cloudflare is giving me a error page.")
                    .SetFileLogContent(
                        $"Tried to change banner and recieved a {ex.StatusCode} status code when attempting to ask Manebooru for the featured image. Doing nothing.\n" +
                        $"Likely causes:\n" +
                        $"  - I sent a badly formatted request to Manebooru.\n" +
                        $"  - Manebooru thinks I sent a badly formatted request when I didn't.\n" +
                        $"  - Manebooru is down and Cloudflare is giving me a error page.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a general error when attempting to ask Manebooru for the featured image. Doing nothing.\n" +
                        $"Likely causes:\n" +
                        $"  - The image is too big for Discord.\n" +
                        $"  - This server cannot have a banner.\n" +
                        $"  - The banner rotation job is an unexpected state.")
                    .SetFileLogContent(
                        $"Tried to change banner and received a general error when attempting to ask Manebooru for the featured image. Doing nothing.\n" +
                        $"Likely causes:\n" +
                        $"  - The image is too big for Discord.\n" +
                        $"  - This server cannot have a banner.\n" +
                        $"  - The banner rotation job is an unexpected state.")
                    .Send();
                await _logger.Log(
                    $"Encountered exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }

            return true;
        }

        return false;
    }
}
