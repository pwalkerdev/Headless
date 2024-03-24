using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Headless.Core
{
    public class HeadlessService
    {
        // TODO - Dependency injection!
        public IReadOnlyCollection<IReadScripts> ScriptCompilers { get; } = AllExportedTypes.Where(typeof(IReadScripts).IsAssignableFrom).Select(Activator.CreateInstance).Cast<IReadScripts>().ToArray();
        public IReadOnlyCollection<IRunScripts> ScriptInvokers { get; } = AllExportedTypes.Where(typeof(IRunScripts).IsAssignableFrom).Select(Activator.CreateInstance).Cast<IRunScripts>().ToArray();

        public IReadScripts ResolveCompiler(string language, string languageVersion = "latest", string runtime = "any")
        {
            // TODO: Make return the 'closest match' to what the caller requests if/when no compiler with the exact version/runtime is found
            //var all = ScriptCompilers.ToDictionary(k => k, v => v.GetType().GetCustomAttribute<SupportedLanguageAttribute>()).Where(kvp => kvp.Value.Name.Equals(language, StringComparison.Ordinal)).ToDictionary(k => k.Key, v => v.Value);
            //if (all.Count == 1)
            //    return all.ElementAt(0).Key;

            return ScriptCompilers.Select(sc => new { Instance = sc, Supports = sc.GetType().GetCustomAttribute<SupportedLanguageAttribute>() }).FirstOrDefault(info => info.Supports.Name == language && info.Supports.Version == languageVersion && info.Supports.Runtime == runtime)?.Instance;
        }

        public IRunScripts ResolveInvoker(string language, string languageVersion = "latest", string runtime = "any")
        {
            // TODO: Make return the 'closest match' to what the caller requests if/when no compiler with the exact version/runtime is found, also merge this with method above
            //var all = ScriptCompilers.ToDictionary(k => k, v => v.GetType().GetCustomAttribute<SupportedLanguageAttribute>()).Where(kvp => kvp.Value.Name.Equals(language, StringComparison.Ordinal)).ToDictionary(k => k.Key, v => v.Value);
            //if (all.Count == 1)
            //    return all.ElementAt(0).Key;

            return ScriptInvokers.Select(sc => new { Instance = sc, Supports = sc.GetType().GetCustomAttribute<SupportedLanguageAttribute>() }).FirstOrDefault(info => info.Supports.Name == language && info.Supports.Version == languageVersion && info.Supports.Runtime == runtime)?.Instance;
        }

        private static Type[] AllExportedTypes { get; } = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "Headless.*.dll", SearchOption.TopDirectoryOnly).SelectMany(ass => Assembly.LoadFrom(ass).ExportedTypes.Where(t => t.IsClass && !t.IsAbstract)).ToArray();
    }
}
