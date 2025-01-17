using Izzy_Moonbot;
using Izzy_Moonbot.Service;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Services;

public class TestLogger<T> : ILogger<T>
{
    public List<string> Logs = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();

    public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logs.Add(formatter(state, exception));
    }
}

[TestClass()]
public class LoggingServiceTests
{
    [TestMethod()]
    public async Task BasicTests()
    {
        var logger = new TestLogger<Worker>();
        var logService = new LoggingService(logger);

        logService.Log("test");
        TestUtils.AssertListsAreEqual(new List<string> { "[LoggingServiceTests.cs:BasicTests:30] test" }, logger.Logs);

        var (_, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "good morning everypony");

        logService.Log("sunny said something", context);
        TestUtils.AssertListsAreEqual(new List<string> {
            $"[LoggingServiceTests.cs:BasicTests:30] test",
            $"[LoggingServiceTests.cs:BasicTests:36] server: Maretime Bay ({guild.Id}) #general ({generalChannel.Id}) @Sunny ({sunny.Id}), sunny said something"
        }, logger.Logs);
    }
}
