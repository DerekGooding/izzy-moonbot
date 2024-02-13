namespace Izzy_Moonbot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ParameterAttribute(string name, ParameterType type, string summary, bool optional = false) : Attribute
{
    public string Name { get; } = name;
    public ParameterType Type { get; } = type;
    public string Summary { get; } = summary;
    public bool Optional { get; } = optional;

    public override string ToString()
    {
        var typeName = Type switch
        {
            ParameterType.Boolean => "Boolean",
            ParameterType.Character => "Character",
            ParameterType.String => "String",
            ParameterType.Integer => "Integer",
            ParameterType.Double => "Decimal Number",
            ParameterType.UserResolvable => "User ID, @Mention, or Partial Name",
            ParameterType.UnambiguousUser => "User ID or @Mention",
            ParameterType.Role => "Role",
            ParameterType.Channel => "Channel",
            ParameterType.Snowflake => "Snowflake ID",
            ParameterType.DateTime => "Date/Time",
            _ => "Unknown"
        };

        if (Type == ParameterType.Complex)
        {
            return "/!\\ This commands parameters change depending on the input provided.\n" +
                   "Please run this command without any arguments to view it's usage.";
        }

        return $"{Name} [{typeName}]{(Optional ? " {OPTIONAL}" : "")} - {Summary}";
    }
}

public enum ParameterType
{
    Boolean,
    Character,
    String,
    Integer,
    Double,
    UserResolvable,
    UnambiguousUser,
    Role,
    Channel,
    Snowflake,
    DateTime,
    Complex
}
