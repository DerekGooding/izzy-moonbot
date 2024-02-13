namespace Izzy_Moonbot.Helpers;

public class DateTimeHelper
{
    public static DateTimeOffset? FakeUtcNow { get; set; } = null;

    public static DateTimeOffset UtcNow => FakeUtcNow is DateTimeOffset now ? now : DateTimeOffset.UtcNow;
}
