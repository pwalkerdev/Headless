using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Headless.Core
{
    public class HeadlessService
    {
        private IReadOnlyCollection<IReadScripts> _scriptCompilers;
        private IReadOnlyCollection<IRunScripts> _scriptInvokers;

        public IReadOnlyCollection<IReadScripts> ScriptCompilers => _scriptCompilers = _scriptCompilers ?? GetScriptCompilers();
        public IReadOnlyCollection<IRunScripts> ScriptInvokers => _scriptInvokers = _scriptInvokers ?? GetScriptInvokers();

        private static IReadOnlyCollection<string> AssemblyFiles { get; } = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "Headless.*.dll", SearchOption.TopDirectoryOnly).ToArray();
        private static IReadOnlyCollection<Assembly> Assemblies { get; } = AssemblyFiles.Select(Assembly.Load).ToArray();

        private IReadOnlyCollection<IReadScripts> GetScriptCompilers()
        {
            return Assemblies.SelectMany(ass => ass.ExportedTypes.Where(t => t.IsClass && !t.IsAbstract && typeof(IReadScripts).IsAssignableFrom(t))).Cast<IRead>().ToArray();
        }

        private IReadOnlyCollection<IRunScripts> GetScriptInvokers()
        {

        }
    }
}
