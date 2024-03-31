using System;

namespace Headless.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedTargetAttribute : Attribute
    {
        public string Key { get; }

        public SupportedTargetAttribute(string name, string version = "latest", string runtime = "any")
        {
            Key = $"{name}.{version}.{runtime}";
        }

        public override string ToString() => Key;
    }
}
