﻿using Discord;
using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class SpamServiceTests
{
    public void SpamSetup(Config cfg, TestUser spammer, StubChannel modChat, StubGuild guild, StubClient client)
    {
        DiscordHelper.DefaultGuildId = guild.Id;
        DiscordHelper.DevUserIds = new List<ulong>();
        DiscordHelper.PleaseAwaitEvents = true;
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        cfg.ModChannel = modChat.Id;

        // SpamService assumes that every MessageReceived event it receives is for
        // a user who is already in the users Dictionary and has a timestamp
        var users = new Dictionary<ulong, User>();
        users[spammer.Id] = new User();
        users[spammer.Id].Timestamp = DateTimeHelper.UtcNow;

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new SpamService(logger, mod, modLog, cfg, users);

        ss.RegisterEvents(client);
    }

    [TestMethod()]
    public async Task Breathing_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        SpamSetup(cfg, sunny, modChat, guild, client);

        Assert.AreEqual(0, generalChannel.Messages.Count);
        Assert.AreEqual(0, modChat.Messages.Count);

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, SpamService._testString);

        // The spam message has already been deleted
        Assert.AreEqual(0, generalChannel.Messages.Count);

        Assert.AreEqual(1, modChat.Messages.Count);
        Assert.AreEqual("<@&0> Spam detected by <@2>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", "<@2> (`2`)"),
            ("Channel", "<#1>"),
            ("Pressure", "This user's last message raised their pressure from 0 to 60, exceeding 60"),
            ("Breakdown of last message", "**Test string**"),
        });
    }

    [TestMethod()]
    public async Task EachTypeOfSpam_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        SpamSetup(cfg, sunny, modChat, guild, client);

        cfg.SpamBasePressure = 10; // can't zero this or decay stops working
        cfg.SpamMaxPressure = 60;

        var zeroPenalties = () =>
        {
            cfg.SpamLinePressure = 0;
            cfg.SpamLengthPressure = 0;
            cfg.SpamPingPressure = 0;
            cfg.SpamImagePressure = 0;
            cfg.SpamRepeatPressure = 0;
        };

        // first, test base pressure on its own

        zeroPenalties();

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "hi");
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "hi again");
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "hello?");
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "anyone there?");
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "dead chat");
        Assert.AreEqual(0, modChat.Messages.Count);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "so very dead");
        Assert.AreEqual(1, modChat.Messages.Count);

        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 50 to 60, exceeding 60"),
            ("Breakdown of last message", "**Base: 10**"),
        });

        // since Izzy tries to avoid duplicate alarms, we have to post one ordinary message
        // to let pressure fall back below 60 before trying to raise it again
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "sorry");
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(23);

        // test line pressure

        zeroPenalties();
        cfg.SpamLinePressure = 2;

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id,
            $"hi{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
            $"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
            $"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
            $"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
            $"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}i'm new here");

        Assert.AreEqual(2, modChat.Messages.Count);
        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 0 to 60, exceeding 60"),
            ("Breakdown of last message", $"**Lines: 50 ≈ 25 line breaks × 2**{Environment.NewLine}Base: 10"),
        });

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "sorry");
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(23);

        // test character length pressure

        zeroPenalties();
        cfg.SpamLengthPressure = 0.1;

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id,
            "Imagine reading a post, but over the course of it the quality seems to deteriorate and it gets wose an wose, " +
            "where the swenetence stwucture and gwammer rewerts to a pwoint of uttew non swence, an u jus dont wanna wead " +
            "it anymwore (o´ω｀o) awd twa wol owdewl iws jus awfwul (´･ω･`);. bwt tw powost iwswnwt obwer nyet, it gwos own " +
            "an own an own an own. uwu wanyaa stwop weadwing bwut uwu cwant stop wewding, uwu stwartd thwis awnd ur gwoing " +
            "two fwinibsh it nowo mwattew wat! uwu hab mwoxie kwiddowo, bwut uwu wibl gwib ub sowon. " +
            "i cwan wite wike dis fwor owors, swo dwont cwalengbe mii..");

        Assert.AreEqual(3, modChat.Messages.Count);
        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 0 to 68.4, exceeding 60"),
            ("Breakdown of last message", $"**Length: 58.4 ≈ 584 characters × 0.1**{Environment.NewLine}Base: 10"),
        });

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "sorry");
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(23);

        // test ping/mention pressure

        zeroPenalties();
        cfg.SpamPingPressure = 2.5;

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id,
            "<@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234>" +
            "<@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234> <@1234>");

        Assert.AreEqual(4, modChat.Messages.Count);
        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 0 to 60, exceeding 60"),
            ("Breakdown of last message", $"**Mentions: 50 ≈ 20 mentions × 2.5**{Environment.NewLine}Base: 10"),
        });

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "sorry");
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(23);

        // test image/embed/attachment pressure

        zeroPenalties();
        cfg.SpamImagePressure = 8.3;

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "", attachments: new List<IAttachment> {
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
        });

        Assert.AreEqual(5, modChat.Messages.Count);
        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 0 to 68.1, exceeding 60"),
            ("Breakdown of last message", $"**Embeds: 58.1 ≈ 7 embeds × 8.3**{Environment.NewLine}Base: 10"),
        });

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "sorry");
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddHours(23);

        // test repeat pressure

        zeroPenalties();
        cfg.SpamRepeatPressure = 20;

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "hi");
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "hi");
        Assert.AreEqual(5, modChat.Messages.Count);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "hi");

        Assert.AreEqual(6, modChat.Messages.Count);
        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 40 to 70, exceeding 60"),
            ("Breakdown of last message", $"**Repeat of Previous Message: 20**{Environment.NewLine}Base: 10"),
        });
    }

    [TestMethod()]
    public async Task EveryTypeOfSpamAtOnce_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        SpamSetup(cfg, sunny, modChat, guild, client);

        cfg.SpamBasePressure = 10;
        cfg.SpamMaxPressure = 60;
        cfg.SpamLinePressure = 2;
        cfg.SpamLengthPressure = 0.1;
        cfg.SpamPingPressure = 2.5;
        cfg.SpamImagePressure = 8.3;
        cfg.SpamRepeatPressure = 20;

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $"<@1234> <@1234> <@1234>{Environment.NewLine}red feather yellow feather");
        Assert.AreEqual(0, modChat.Messages.Count);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $"<@1234> <@1234> <@1234>{Environment.NewLine}red feather yellow feather", attachments: new List<IAttachment> {
            new TestAttachment(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes("adoptable character art")), "buy now!")),
        });
        Assert.AreEqual(1, modChat.Messages.Count);

        Assert.AreEqual($"<@&0> Spam detected by <@{sunny.Id}>", modChat.Messages.Last().Content);
        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Pressure", "This user's last message raised their pressure from 24.6 to 77.5, exceeding 60"),
            ("Breakdown of last message",
                $"**Repeat of Previous Message: 20**{Environment.NewLine}" +
                $"Base: 10{Environment.NewLine}" +
                $"Embeds: 8.3 ≈ 1 embeds × 8.3{Environment.NewLine}" +
                $"Mentions: 7.5 ≈ 3 mentions × 2.5{Environment.NewLine}" +
                $"Length: 5.1 ≈ 51 characters × 0.1{Environment.NewLine}" +
                $"Lines: 2 ≈ 1 line breaks × 2"),
        });
    }
}