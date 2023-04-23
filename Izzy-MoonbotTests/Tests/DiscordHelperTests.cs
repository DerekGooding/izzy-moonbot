﻿using Izzy_Moonbot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Izzy_Moonbot.Helpers.DiscordHelper;

namespace Izzy_Moonbot_Tests.Helpers;

[TestClass()]
public class DiscordHelperTests
{
    [TestMethod()]
    public void MiscTests()
    {
        Assert.IsTrue(DiscordHelper.IsSpace(' '));
        Assert.IsFalse(DiscordHelper.IsSpace('a'));
    }

    [TestMethod()]
    public void StripQuotesTests()
    {
        Assert.AreEqual("", DiscordHelper.StripQuotes(""));
        Assert.AreEqual("a", DiscordHelper.StripQuotes("a"));
        Assert.AreEqual("ab", DiscordHelper.StripQuotes("ab"));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("foo"));
        Assert.AreEqual("foo bar", DiscordHelper.StripQuotes("foo bar"));
        Assert.AreEqual("foo \"bar\" baz", DiscordHelper.StripQuotes("foo \"bar\" baz"));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("\"foo\""));
        Assert.AreEqual("foo bar", DiscordHelper.StripQuotes("\"foo bar\""));
        Assert.AreEqual("foo \"bar\" baz", DiscordHelper.StripQuotes("\"foo \"bar\" baz\""));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("'foo'"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("ʺfooʺ"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("˝fooˮ"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("“foo”"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("'foo”"));
    }

    [TestMethod()]
    public void ConvertPingsTests()
    {
        Assert.AreEqual(0ul, DiscordHelper.ConvertChannelPingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertChannelPingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertChannelPingToId("<#1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertChannelPingToId("<#>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertChannelPingToId("foo <#1234> bar"));

        Assert.AreEqual(0ul, DiscordHelper.ConvertUserPingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertUserPingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertUserPingToId("<@1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertUserPingToId("<@>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertUserPingToId("foo <@1234> bar"));

        Assert.AreEqual(0ul, DiscordHelper.ConvertRolePingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertRolePingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertRolePingToId("<@&1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertRolePingToId("<@&>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertRolePingToId("foo <@&1234> bar"));
    }

    void AssertArgumentResultsAreEqual(ArgumentResult expected, ArgumentResult actual)
    {
        TestUtils.AssertListsAreEqual(expected.Arguments, actual.Arguments, "\nArguments");
        TestUtils.AssertListsAreEqual(expected.Indices, actual.Indices, "\nIndices");
    }

    string SkippedArgsString(string argsString, int argsToSkip)
    {
        var args = DiscordHelper.GetArguments(argsString);
        return string.Join("", argsString.Skip(args.Indices[argsToSkip]));
    }

    [TestMethod()]
    public void GetArguments_NoQuotesTests()
    {
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = Array.Empty<string>(), Indices = Array.Empty<int>() }, DiscordHelper.GetArguments(""));

        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = Array.Empty<string>(), Indices = Array.Empty<int>() }, DiscordHelper.GetArguments(" "));

        var argsString = "foo";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo" }, Indices = new[] { 3 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "foo ";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo" }, Indices = new[] { 4 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = " foo";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "foo" }, Indices = new[] { 4 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "foo bar";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 7 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo    bar";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 7, 10 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo bar   ";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 10 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar   ", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "   foo bar";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "foo", "bar" }, Indices = new[] { 7, 10 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo baaaar";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "baaaar" }, Indices = new[] { 4, 10 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("baaaar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo bar baz";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar", "baz" }, Indices = new[] { 4, 8, 11 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar baz", SkippedArgsString(argsString, 0));
        Assert.AreEqual("baz", SkippedArgsString(argsString, 1));
        Assert.AreEqual("", SkippedArgsString(argsString, 2));

        argsString = "foo   bar   baz";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar", "baz" }, Indices = new[] { 6, 12, 15 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar   baz", SkippedArgsString(argsString, 0));
        Assert.AreEqual("baz", SkippedArgsString(argsString, 1));
        Assert.AreEqual("", SkippedArgsString(argsString, 2));

        argsString = "   foo   bar   baz   ";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "foo", "bar", "baz" }, Indices = new[] { 9, 15, 21 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar   baz   ", SkippedArgsString(argsString, 0));
        Assert.AreEqual("baz   ", SkippedArgsString(argsString, 1));
        Assert.AreEqual("", SkippedArgsString(argsString, 2));
    }

    [TestMethod()]
    public void GetArguments_QuotesTests()
    {
        var argsString = "\"\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "" }, Indices = new[] { 2 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "\"foo\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo" }, Indices = new[] { 5 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "\"foo bar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo bar" }, Indices = new[] { 9 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "foo \"bar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 9 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"bar\"", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo \"bar baz\" quux";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar baz", "quux" }, Indices = new[] { 4, 14, 18 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"bar baz\" quux", SkippedArgsString(argsString, 0));
        Assert.AreEqual("quux", SkippedArgsString(argsString, 1));
        Assert.AreEqual("", SkippedArgsString(argsString, 2));
    }

    [TestMethod()]
    public void GetArguments_EscapedQuotesTests()
    {
        // TODO: if we ever upgrade to .NET 7 & C# 11, *please* use raw string literals here
        var argsString = "\"\\\"\""; // = [ ", \, ", " ]
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "\\\"" }, Indices = new[] { 4 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "\"foo\\\"bar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "foo\\\"bar" }, Indices = new[] { 10 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "\"fo\\\"o b\\\"ar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "fo\\\"o b\\\"ar" }, Indices = new[] { 13 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "foo\\\" \"bar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "foo\\\"", "bar" }, Indices = new[] { 6, 11 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"bar\"", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo \"bar baz\\\"\" quux";
        AssertArgumentResultsAreEqual(new ArgumentResult { Arguments = new[] { "foo", "bar baz\\\"", "quux" }, Indices = new[] { 4, 16, 20 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"bar baz\\\"\" quux", SkippedArgsString(argsString, 0));
        Assert.AreEqual("quux", SkippedArgsString(argsString, 1));
        Assert.AreEqual("", SkippedArgsString(argsString, 2));
    }

    [TestMethod()]
    public async Task UserRoleChannel_GettersTests()
    {
        var (_, _, (izzyHerself, _), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, izzyHerself.Id, "hello");

        Assert.AreEqual(generalChannel.Id, await GetChannelIdIfAccessAsync($"{generalChannel.Id}", context));
        Assert.AreEqual(0ul, await GetChannelIdIfAccessAsync("999", context));

        Assert.AreEqual(generalChannel.Id, await GetChannelIdIfAccessAsync($"<#{generalChannel.Id}>", context));
        Assert.AreEqual(0ul, await GetChannelIdIfAccessAsync("<#999>", context));

        Assert.AreEqual(generalChannel.Id, await GetChannelIdIfAccessAsync("general", context));
        Assert.AreEqual(0ul, await GetChannelIdIfAccessAsync("other", context));

        Assert.AreEqual(1ul, GetRoleIdIfAccessAsync("1", context));
        Assert.AreEqual(0ul, GetRoleIdIfAccessAsync("999", context));

        Assert.AreEqual(1ul, GetRoleIdIfAccessAsync("<@&1>", context));
        Assert.AreEqual(0ul, GetRoleIdIfAccessAsync("<@&999>", context));

        Assert.AreEqual(1ul, GetRoleIdIfAccessAsync("Alicorn", context));
        Assert.AreEqual(0ul, GetRoleIdIfAccessAsync("other", context));

        // unlike the channel and role getters, this user method intentionally supports "unknown" users not in the guild
        Assert.AreEqual(1ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("1", context));
        Assert.AreEqual(999ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("999", context));

        Assert.AreEqual(1ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("<@1>", context));
        Assert.AreEqual(999ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("<@999>", context));

        Assert.AreEqual(1ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("Izzy", context));
        Assert.AreEqual(2ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("Sunny", context));
        Assert.AreEqual(0ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("other", context));
    }

    [TestMethod()]
    public void TrimDiscordWhitespace_Tests()
    {
        Assert.AreEqual("", TrimDiscordWhitespace(""));
        Assert.AreEqual("", TrimDiscordWhitespace("\n"));
        Assert.AreEqual("", TrimDiscordWhitespace("\n\n\n"));
        Assert.AreEqual("", TrimDiscordWhitespace(":blank:"));
        Assert.AreEqual("", TrimDiscordWhitespace(":blank::blank::blank:"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy\n"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\nIzzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\nIzzy\n"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy:blank:"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace(":blank:Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace(":blank:Izzy:blank:"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n:blank:Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace(":blank:\nIzzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy\n:blank:"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy:blank:\n"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n:blank:Izzy\n:blank:"));

        Assert.AreEqual("IzzyIzzyIzzy", TrimDiscordWhitespace("\n:blank: \n:blank: \nIzzyIzzyIzzy\n"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("<:blank:833008517257756752>Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy<:blank:833008517257756752>"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("<:blank:833008517257756752>Izzy<:blank:833008517257756752>"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n<:blank:833008517257756752>Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("<:blank:833008517257756752>\nIzzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy\n<:blank:833008517257756752>"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy<:blank:833008517257756752>\n"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n<:blank:833008517257756752>Izzy\n<:blank:833008517257756752>"));

        Assert.AreEqual("IzzyIzzyIzzy", TrimDiscordWhitespace("\n<:blank:833008517257756752> \n<:blank:833008517257756752> \nIzzyIzzyIzzy\n"));

    }

    [TestMethod()]
    public void LevenshteinDistance_Tests()
    {
        Assert.IsTrue(WithinLevenshteinDistanceOf("", "", 0));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izzy", 0));

        Assert.IsFalse(WithinLevenshteinDistanceOf("", "Izzy", 0));
        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "", 0));
        Assert.IsFalse(WithinLevenshteinDistanceOf("", "Izzy", 3));
        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "", 3));
        Assert.IsTrue(WithinLevenshteinDistanceOf("", "Izzy", 4));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "", 4));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "Iggy", 1));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Iggy", 2));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Iggy", 3));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "Izzy!", 0));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izzy!", 1));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izzy!", 2));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "Izz", 0));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izz", 1));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izz", 2));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "izy!", 2));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "izy!", 3));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "izy!", 4));

        Assert.IsFalse(WithinLevenshteinDistanceOf("SpamMaxPressure", "SpamPressureMax", 5));
        Assert.IsTrue(WithinLevenshteinDistanceOf("SpamMaxPressure", "SpamPressureMax", 6));
        Assert.IsTrue(WithinLevenshteinDistanceOf("SpamMaxPressure", "SpamPressureMax", 7));
    }
}