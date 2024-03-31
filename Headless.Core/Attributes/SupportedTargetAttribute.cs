using System;

namespace Headless.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedTargetAttribute(string name, string version = "latest", string runtime = "any") : Attribute
    {
        public string Key { get; } = $"{name}.{version}.{runtime}";
        public override string ToString() => Key;
    }
}
