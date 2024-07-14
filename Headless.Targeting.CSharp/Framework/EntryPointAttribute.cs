namespace Headless.Targeting.CSharp.Framework;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate)]
public sealed class EntryPointAttribute(params object[]? arguments) : Attribute
{
    public object[]? Arguments { get; } = arguments;
}