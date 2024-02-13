using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Helpers;

[TestClass()]
public class ConfigHelperTests
{
    [TestMethod()]
    public void Config_GetValueTests()
    {
        var cfg = new Config();
        Assert.AreEqual("you all soon", ConfigHelper.GetValue(cfg, "DiscordActivityName"));
        Assert.AreEqual('.', ConfigHelper.GetValue(cfg, "Prefix"));
        Assert.AreEqual(false, ConfigHelper.GetValue(cfg, "ManageNewUserRoles"));
        Assert.AreEqual(100, ConfigHelper.GetValue(cfg, "UnicycleInterval"));
        Assert.IsTrue(ConfigHelper.GetValue(cfg, "FilterIgnoredChannels") is HashSet<ulong>);
        Assert.IsTrue(ConfigHelper.GetValue(cfg, "Aliases") is Dictionary<string, string>);

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetValue(cfg, "foo"));
    }

    [TestMethod()]
    public async Task Config_SetValue_ValidScalars_TestsAsync()
    {
        var cfg = new Config();

        Assert.AreEqual("you all soon", cfg.DiscordActivityName);
        await ConfigHelper.SetSimpleValue(cfg, "DiscordActivityName", "the hoofball game");
        Assert.AreEqual("the hoofball game", cfg.DiscordActivityName);

        Assert.AreEqual('.', cfg.Prefix);
        await ConfigHelper.SetSimpleValue(cfg, "Prefix", '!');
        Assert.AreEqual('!', cfg.Prefix);

        Assert.AreEqual(false, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "y");
        Assert.AreEqual(true, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "false");
        Assert.AreEqual(false, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "enable");
        Assert.AreEqual(true, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "deactivate");
        Assert.AreEqual(false, cfg.ManageNewUserRoles);

        Assert.AreEqual(100, cfg.UnicycleInterval);
        await ConfigHelper.SetSimpleValue(cfg, "UnicycleInterval", 42);
        Assert.AreEqual(42, cfg.UnicycleInterval);

        Assert.AreEqual(10.0, cfg.SpamBasePressure);
        await ConfigHelper.SetSimpleValue(cfg, "SpamBasePressure", 0.5);
        Assert.AreEqual(0.5, cfg.SpamBasePressure);

        Assert.AreEqual(ConfigListener.BannerMode.None, cfg.BannerMode);
        await ConfigHelper.SetSimpleValue(cfg, "BannerMode", ConfigListener.BannerMode.ManebooruFeatured);
        Assert.AreEqual(ConfigListener.BannerMode.ManebooruFeatured, cfg.BannerMode);
    }

    // TODO: figure out Discord.NET test doubles to enable testing users, roles, channels, etc
    /*[TestMethod()]
    public async Task Config_SetValue_ValidDiscordEntitiesTestsAsync()
    {
    }*/

    [TestMethod()]
    public void Config_SetValue_InvalidValues_Tests()
    {
        var cfg = new Config();

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetSimpleValue(cfg, "foo", "bar"));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetSimpleValue(cfg, "Aliases", "bar"));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetSimpleValue(cfg, "foo", 'b'));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetSimpleValue(cfg, "Aliases", 'b'));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetBooleanValue(cfg, "foo", "bar"));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetBooleanValue(cfg, "Aliases", "bar"));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetSimpleValue(cfg, "foo", 42));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetSimpleValue(cfg, "Aliases", 42));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetSimpleValue(cfg, "foo", 1.0));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetSimpleValue(cfg, "Aliases", 1.0));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetSimpleValue(cfg, "foo", ConfigListener.BannerMode.ManebooruFeatured));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetSimpleValue(cfg, "Aliases", ConfigListener.BannerMode.ManebooruFeatured));
    }

    // The built-in Assert.AreEqual and CollectionsAssert.AreEqual have error messages so bad it was worth writing my own asserts
    //void AssertListsAreEqual<T>(IList<T>? expected, IList<T>? actual, string message = "")
    //{
    //    if (expected is null || actual is null)
    //    {
    //        Assert.AreEqual(expected, actual);
    //        return;
    //    }
    //    if (expected.Count != actual.Count)
    //        Assert.AreEqual(expected, actual, $"\nCount() mismatch: {expected.Count} != {actual.Count}");
    //    foreach (var i in Enumerable.Range(0, expected.Count))
    //        Assert.AreEqual(expected[i], actual[i], $"\nItem {i}" + message);
    //}

    [TestMethod()]
    public async Task Config_HashSets_TestsAsync()
    {
        var cfg = new Config();

        TestUtils.AssertSetsAreEqual(new HashSet<string>(), cfg.BannerImages);
        TestUtils.AssertSetsAreEqual(new HashSet<string>(), ConfigHelper.GetStringSet(cfg, "BannerImages"));
        Assert.IsFalse(ConfigHelper.DoesDictionaryKeyExist<string>(cfg, "BannerImages", "manebooru.art/images/1"));

        await ConfigHelper.AddToStringSet(cfg, "BannerImages", "manebooru.art/images/1");

        TestUtils.AssertSetsAreEqual(new HashSet<string> { "manebooru.art/images/1" }, cfg.BannerImages);
        TestUtils.AssertSetsAreEqual(new HashSet<string> { "manebooru.art/images/1" }, ConfigHelper.GetStringSet(cfg, "BannerImages"));
        Assert.IsTrue(ConfigHelper.HasValueInSet(cfg, "BannerImages", "manebooru.art/images/1"));

        await ConfigHelper.RemoveFromStringSet(cfg, "BannerImages", "manebooru.art/images/1");

        TestUtils.AssertSetsAreEqual(new HashSet<string>(), cfg.BannerImages);
        TestUtils.AssertSetsAreEqual(new HashSet<string>(), ConfigHelper.GetStringSet(cfg, "BannerImages"));
        Assert.IsFalse(ConfigHelper.HasValueInSet(cfg, "BannerImages", "manebooru.art/images/1"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetStringSet(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetStringSet(cfg, "Prefix"));
    }

    [TestMethod()]
    public async Task Config_DictionariesOfScalars_TestsAsync()
    {
        var cfg = new Config();

        // Aliases is the only Dict<string, string> in Config

        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string>(), ConfigHelper.GetDictionary<string>(cfg, "Aliases"));
        Assert.IsFalse(ConfigHelper.DoesDictionaryKeyExist<string>(cfg, "Aliases", "testalias"));

        await ConfigHelper.CreateDictionaryKey<string>(cfg, "Aliases", "testalias", "echo hi");

        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo hi" } }, cfg.Aliases);
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo hi" } }, ConfigHelper.GetDictionary<string>(cfg, "Aliases"));
        Assert.IsTrue(ConfigHelper.DoesDictionaryKeyExist<string>(cfg, "Aliases", "testalias"));
        Assert.AreEqual("echo hi", ConfigHelper.GetDictionaryValue<string>(cfg, "Aliases", "testalias"));

        await ConfigHelper.SetStringDictionaryValue(cfg, "Aliases", "testalias", "echo belizzle it");

        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo belizzle it" } }, cfg.Aliases);
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo belizzle it" } }, ConfigHelper.GetDictionary<string>(cfg, "Aliases"));
        Assert.IsTrue(ConfigHelper.DoesDictionaryKeyExist<string>(cfg, "Aliases", "testalias"));
        Assert.AreEqual("echo belizzle it", ConfigHelper.GetDictionaryValue<string>(cfg, "Aliases", "testalias"));

        await ConfigHelper.RemoveDictionaryKey<string>(cfg, "Aliases", "testalias");

        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string>(), ConfigHelper.GetDictionary<string>(cfg, "Aliases"));
        Assert.IsFalse(ConfigHelper.DoesDictionaryKeyExist<string>(cfg, "Aliases", "testalias"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetDictionary<string>(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetDictionary<string>(cfg, "Prefix"));
    }
}
