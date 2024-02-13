using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class QuoteStorage
{
    public QuoteStorage()
    {
        Quotes = [];
        Aliases = [];
    }

    public Dictionary<string, List<string>> Quotes { get; set; }
    public Dictionary<string, string> Aliases { get; set; }
}
