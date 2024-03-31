using System;
using System.Collections.Generic;
using System.Linq;

namespace Headless.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedTargetsAttribute(string name, string versions = "latest", string runtimes = "any") : Attribute
    {
        public const char KeyDelimiter = ';';
        public const char PropertyDelimiter = '|';

        public IEnumerable<string> Keys { get; } = versions.Split(PropertyDelimiter).SelectMany(version => runtimes.Split(PropertyDelimiter).Select(runtime => $"{name}{KeyDelimiter}{version}{KeyDelimiter}{runtime}"));
    }
}
