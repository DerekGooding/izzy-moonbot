namespace Izzy_Moonbot.Types;

public class ConfigValueChangeEvent(string name, object? original, object? current) : EventArgs
{
    public string Name = name;
    public object? Original = original;
    public object? Current = current;

    public override string ToString() => $"{Name}: {Original ?? "NULL"} => {Current ?? "NULL"}";
}
