namespace Headless.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly string[] TargetingDllIdentifiers = ["Headless.Targeting.", ".dll"];
    private static readonly Type[] HeadlessServiceTypes = [typeof(IScriptCompiler), typeof(IScriptInvoker)];
    private static readonly Func<Type, bool> IsHeadlessService = type => type.GetInterfaces().Any(HeadlessServiceTypes.Contains);

    private static Assembly? ResolveHeadlessServiceAssembly(object? sender, ResolveEventArgs args)
    {
        if (AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(n => n.FullName == args.Name) is { } alreadyLoadedAssembly)
            return alreadyLoadedAssembly;

        var name = new AssemblyName(args.Name).Name;
        var embeddedAssemblies = Assembly.GetEntryAssembly()?.GetManifestResourceNames().ToArray() ?? Array.Empty<string>();
        if (embeddedAssemblies.FirstOrDefault(n => n == $"{name}.dll") is { Length: > 0 } assemblyResourceName
            && Assembly.GetEntryAssembly()?.GetManifestResourceStream(assemblyResourceName) is { Length: > 0 } stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return Assembly.Load(ms.ToArray());
        }

        var assemblyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{name}.dll");
        return File.Exists(assemblyFileName) ? Assembly.LoadFrom(assemblyFileName) : null;
    }

    public static IServiceCollection AddHeadlessServices(this IServiceCollection services, HostBuilderContext hostBuilder)
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveHeadlessServiceAssembly;

        var serviceInfos = Assembly.GetCallingAssembly().GetManifestResourceNames()
            .Where(name => name.StartsWith(TargetingDllIdentifiers[0]) && name.EndsWith(TargetingDllIdentifiers[1]))
            .Select(Path.GetFileNameWithoutExtension)
            .Select(Assembly.Load!)
            .SelectMany(ass => ass.ExportedTypes.Where(type => type is { IsClass: true, IsAbstract: false }).Where(IsHeadlessService))
            .Select(type => new
            {
                Type = type,
                ImplementedInterfaces = type.GetInterfaces().Where(HeadlessServiceTypes.Contains).ToArray(),
                SupportedTargets = type.GetCustomAttribute<SupportedTargetsAttribute>()?.Keys.ToArray()
            })
            .Where(infos => infos.SupportedTargets is { Length: > 0 })
            .ToArray();
        
        // This will register all services by their interface and keyed by their supported target attributes
        // If a service inherits more than 1 recognised interface and/or more than 1 supported target,
        // all requests for any of the matching criteria will return the same instance per DI scope
        foreach (var (serviceInfo, @interface, key) in serviceInfos.SelectMany(serviceInfo => serviceInfo.ImplementedInterfaces.SelectMany(@interface => serviceInfo.SupportedTargets!.Select(key => (serviceInfo, @interface, key)))))
            _ = services.Any(s => s.IsKeyedService && s.KeyedImplementationType == serviceInfo.Type) ? services.AddKeyedScoped(@interface, key, (p, _) => p.GetKeyedServices(serviceInfo.ImplementedInterfaces[0], serviceInfo.SupportedTargets![0]).Single(s => s != null)!) : services.AddKeyedScoped(@interface, key, serviceInfo.Type);

        return services
            .Configure<CommandLineOptions>(hostBuilder.Configuration)
            .Configure<JavaScriptInterpreterOptions>(hostBuilder.Configuration.GetSection("JavaScriptInterpreter"))
            .Configure<CSharpScriptInterpreterOptions>(hostBuilder.Configuration.GetSection("CSharpScriptInterpreter"))
            .AddSingleton<CommandLineOptions>(provider => provider.GetRequiredService<IOptions<CommandLineOptions>>().Value)
            .AddSingleton<CSharpScriptInterpreterOptions>(provider => provider.GetRequiredService<IOptions<CSharpScriptInterpreterOptions>>().Value)
            .AddSingleton<JavaScriptInterpreterOptions>(provider => provider.GetRequiredService<IOptions<JavaScriptInterpreterOptions>>().Value);
    }

    public static void AddHeadlessServices(HostBuilderContext hostBuilder, IServiceCollection services) => services.AddHeadlessServices(hostBuilder);
}