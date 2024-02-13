using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Flurl.Http;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Modules;

[Summary("Development commands.")]
public class DevModule(Config config, Dictionary<ulong, User> users, FilterService filterService,
    LoggingService loggingService, ModLoggingService modLoggingService, ModService modService,
    SpamService pressureService, RaidService raidService, ScheduleService scheduleService, TransientState state) : ModuleBase<SocketCommandContext>
{
    private readonly FilterService _filterService = filterService;
    private readonly LoggingService _loggingService = loggingService;
    private readonly ModLoggingService _modLoggingService = modLoggingService;
    private readonly ModService _modService = modService;
    private readonly SpamService _pressureService = pressureService;
    private readonly RaidService _raidService = raidService;
    private readonly ScheduleService _scheduleService = scheduleService;
    private readonly Config _config = config;
    private readonly TransientState _state = state;
    private readonly Dictionary<ulong, User> _users = users;

    [NamedArgumentType]
    public class TypeTestArguments
    {
        public bool? Boolean { get; set; }
        public char? Character { get; set; }
        public byte? Nom { get; set; }
        public short? Pipp { get; set; }
        public int? Integer { get; set; }
        public long? Starlight { get; set; }
        public double? Rainboom { get; set; }
        public string? Single { get; set; }
        public string? Multiword { get; set; }
        public TestEnum? How { get; set; }
        public DateTimeOffset? Time { get; set; }
        public SocketTextChannel? Channel { get; set; }
        public SocketGuildUser? User { get; set; }
        public SocketUserMessage? Message { get; set; }
        public SocketRole? Role { get; set; }
    }

    [Command("typetest")]
    [Summary("Type testing")]
    [DevCommand]
    public async Task TypeTestCommandAsync(TypeTestArguments tests)
    {
        var testsCompleted = new Dictionary<string, string?>();

        if (tests.Boolean.HasValue) testsCompleted.Add("bool", tests.Boolean.Value.ToString());
        if (tests.Character.HasValue) testsCompleted.Add("char", tests.Character.Value.ToString());
        if (tests.Nom.HasValue) testsCompleted.Add("byte", tests.Nom.Value.ToString());
        if (tests.Pipp.HasValue) testsCompleted.Add("short", tests.Pipp.Value.ToString());
        if (tests.Integer.HasValue) testsCompleted.Add("int", tests.Integer.Value.ToString());
        if (tests.Starlight.HasValue) testsCompleted.Add("long", tests.Starlight.Value.ToString());
        if (tests.Rainboom.HasValue) testsCompleted.Add("double", tests.Rainboom.Value.ToString());
        if (tests.Single != null) testsCompleted.Add("string (single)", tests.Single);
        if (tests.Multiword != null) testsCompleted.Add("string (multiple)", tests.Multiword);
        if (tests.How.HasValue) testsCompleted.Add("enum", tests.How.Value.ToString());
        if (tests.Time.HasValue) testsCompleted.Add("datetimeoffset", tests.Time.Value.ToString());
        if (tests.Channel != null) testsCompleted.Add("channel", tests.Channel.Name);
        if (tests.User != null) testsCompleted.Add("user", tests.User.DisplayName);
        if (tests.Message != null) testsCompleted.Add("message", tests.Message.GetJumpUrl());
        if (tests.Role != null) testsCompleted.Add("role", tests.Role.Name);

        var resultToPrint = testsCompleted.Select(pair => $"{pair.Key}: {pair.Value}");

        await ReplyAsync($"Type testing results: \n" +
                         $"{string.Join('\n', resultToPrint)}");
    }

    public enum TestEnum
    {
        Hello,
        Goodbye,
        None
    }

    [Command("test")]
    [Summary("Unit tests for Izzy Moonbow")]
    [DevCommand]
    public async Task TestCommandAsync([Summary("Test Identifier")] string testId = "",
        [Remainder] [Summary("Test arguments")]
        string argString = "")
    {
        var args = argString.Split(" ");
        switch (testId)
        {
            case "pagination":
                var pages =
                    "Hello!||This is a test of pagination!||If this works, you're able to see this.||The paginated message will expire in 5 minutes.||Hopefully my code isn't broken..."
                        .Split("||");
                var staticParts =
                    $"**Test utility** - Pagination test\n*This is a simple test for the pagination utility!*\n*This is a header which will remain regardless of the current page.*\nBelow is the paginated content.||This is the footer of the pagination message which will remain regardless of the current page\nThere is a countdown below as well as buttons to change the page."
                        .Split("||");

                var paginationHelper = new PaginationHelper(Context, pages, staticParts);
                break;

            case "dump-users-size":
                await Context.Message.ReplyAsync($"UserStore size: {_users.Count}");
                break;

            case "create-echo-task":
                var action = new ScheduledEchoJob(Context.Channel.Id,
                    "Hello! Exactly 1 minute should have passed between the test command and this message!");
                var task = new ScheduledJob(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1), action);
                await _scheduleService.CreateScheduledJob(task);
                await Context.Message.ReplyAsync("Created scheduled task.");
                break;

            case "test-twilight":
                await Context.Channel.SendMessageAsync(
                    $"Dear Princess Twilight,\n```\n" +
                    $"[2022-07-30 00:19:07 ERR] Izzy Moonbot has encountered an error. Logging information...\n" +
                    $"[2022-07-30 00:19:07 ERR] Message: Server requested a reconnect\n" +
                    $"[2022-07-30 00:19:07 ERR] Source: System.Private.CoreLib\n" +
                    $"[2022-07-30 00:19:07 ERR] HResult: -2146233088\n" +
                    "[2022-07-30 00:19:07 ERR] Stack trace:    at Discord.ConnectionManager.<>c__DisplayClass29_0.<<StartAsync>b__0>d.MoveNext()" +
                    $"\n```Your faithful Bot,\nIzzy Moonbot");
                break;

            case "twilight":
                await Context.Guild.GetTextChannel(1002687344199094292).SendMessageAsync(
                    $"Dear Princess Twilight,\n```\n" +
                    $"[2022-07-30 00:19:07 ERR] Izzy Moonbot has encountered an error. Logging information...\n" +
                    $"[2022-07-30 00:19:07 ERR] Message: Server requested a reconnect\n" +
                    $"[2022-07-30 00:19:07 ERR] Source: System.Private.CoreLib\n" +
                    $"[2022-07-30 00:19:07 ERR] HResult: -2146233088\n" +
                    "[2022-07-30 00:19:07 ERR] Stack trace:    at Discord.ConnectionManager.<>c__DisplayClass29_0.<<StartAsync>b__0>d.MoveNext()" +
                    $"\n```Your faithful Bot,\nIzzy Moonbot");

                break;

            case "state":
                _state.CurrentSmallJoinCount++;
                await ReplyAsync($"At {_state.CurrentSmallJoinCount}.");
                break;

            case "asyncSyncTesk":
                Console.WriteLine("Application executing on thread {0}",
                    Environment.CurrentManagedThreadId);
                var asyncTask = Task.Run(() =>
                {
                    Console.WriteLine("Task {0} (asyncTask) executing on Thread {1}",
                        Task.CurrentId,
                        Environment.CurrentManagedThreadId);
                    long sum = 0;
                    for (int ctr = 1; ctr <= 1000000; ctr++)
                        sum += ctr;
                    return sum;
                });
                var syncTask = new Task<long>(() =>
                {
                    Console.WriteLine("Task {0} (syncTask) executing on Thread {1}",
                        Task.CurrentId,
                        Environment.CurrentManagedThreadId);
                    long sum = 0;
                    for (int ctr = 1; ctr <= 1000000; ctr++)
                        sum += ctr;
                    return sum;
                });
                syncTask.RunSynchronously();
                Console.WriteLine();
                Console.WriteLine("Task {0} returned {1:N0}", syncTask.Id, syncTask.Result);
                Console.WriteLine("Task {0} returned {1:N0}", asyncTask.Id, asyncTask.Result);
                break;

            case "overloadFilter":
                {
                    var izzyContext = new SocketCommandContextAdapter(Context);
                    for (var i = 0; i < 10; i++)
                    {
                        var _ = Task.Run(async () => await _filterService.ProcessMessage(izzyContext.Message, izzyContext.Client));
                    }
                    break;
                }
            case "logTest":
                var pressureTracer = new Dictionary<string, double> { { "Base", _config.SpamBasePressure } };
                _loggingService.Log($"Pressure increase by 0 to 0/{_config.SpamMaxPressure}.\n                          Pressure trace: {string.Join(", ", pressureTracer)}", Context, level: LogLevel.Debug);
                break;

            case "invitesDisabled":
                await ReplyAsync("Invites disabled: " + Context.Guild.Features.HasFeature("INVITES_DISABLED"));
                break;

            case "parsehelper":
                if (ParseHelper.TryParseDateTime(argString, out var err) is not var (time, remainingArgsString))
                {
                    await ReplyAsync($"TryParseDateTime returned null for \"{argString}\"\n" +
                        $"with error message: {err}");
                    return;
                }

                await ReplyAsync(
                    $"TryParseDateTime converted \"{argString}\" to a DateTimeOffset of {time.Time}" +
                        $"{(time.RepeatType is ScheduledJobRepeatType.None ? "" : $" repeating {time.RepeatType}")}\n" +
                    $"with remainingArgsString: \"{remainingArgsString}\"");
                break;

            case "customArgument":
                var customArgument_Result = DiscordHelper.GetArguments(argString);

                await ReplyAsync($"Processed. Here's what I got:\n```\n" +
                                 $"{string.Join(", ", customArgument_Result)}" +
                                 $"\n```");
                break;

            case "listEnum":
                var enumNames = Enum.GetNames<TestEnum>();

                await ReplyAsync($"```\n{string.Join(", ", enumNames)}\n```");
                break;

            case "parseEnum":
                if (!Enum.TryParse<TestEnum>(args[0], out var testEnum))
                {
                    await ReplyAsync("Parse fail.");
                    return;
                }

                await ReplyAsync($"Parse success. `{testEnum}`");
                break;

            case "parseImage":
                var attachment = new FileAttachment(args[0].GetStreamAsync().Result, "test.png");

                await Context.Channel.SendFileAsync(attachment, "Test Success");
                break;

            default:
                await Context.Message.ReplyAsync("Unknown test.");
                break;
        }
    }
}
