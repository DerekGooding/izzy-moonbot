namespace Izzy_Moonbot.Describers;

public class ConfigItem(string name, ConfigItemType type, string description, ConfigItemCategory category,
    bool nullable = false)
{
    public string Name { get; } = name;
    public ConfigItemType Type { get; } = type;
    public string Description { get; } = description;
    public ConfigItemCategory Category { get; } = category;
    public bool Nullable { get; } = nullable;
}

public enum ConfigItemCategory
{
    Setup,
    Misc,
    Banner,
    ManagedRoles,
    Filter,
    Spam,
    Raid,
    Bored,
    Witty,
    Monitoring
}

public enum ConfigItemType
{
    // Values
    String,

    Char,
    Boolean,
    Integer,
    UnsignedInteger,
    Double,
    Enum,
    Role,
    Channel,

    // Sets
    StringSet,

    RoleSet,
    ChannelSet,

    // Dictionaries
    StringDictionary,

    // Dictionaries of Sets
    StringSetDictionary
}
