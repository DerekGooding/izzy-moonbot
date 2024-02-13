namespace Izzy_Moonbot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ExampleAttribute(string text) : Attribute
{
    public string Text { get; } = text;

    public override string ToString() => Text;
}
