using System;

namespace Headless.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedLanguageAttribute : System.Attribute
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Runtime { get; set; }

        public SupportedLanguageAttribute(string name, string version = "latest", string runtime = "any")
        {
            Name = name;
            Version = version;
            Runtime = runtime;
        }
    }
}
