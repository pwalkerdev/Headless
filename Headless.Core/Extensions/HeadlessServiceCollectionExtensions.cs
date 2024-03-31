using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Headless.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Headless.Core.Extensions
{
    public static class HeadlessServiceCollectionExtensions
    {
        private const string TargetingDllWildcard = "Headless.Targeting.*.dll";
        private static readonly Type[] HeadlessServiceTypes = [typeof(IReadScripts), typeof(IRunScripts)];
        private static readonly Func<Type, bool> IsHeadlessService = type => type.GetInterfaces().Any(HeadlessServiceTypes.Contains);

        public static IServiceCollection AddHeadlessService(this IServiceCollection services)
        {
            var serviceInfos = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, TargetingDllWildcard)
                .Select(Assembly.LoadFrom)
                .SelectMany(ass => ass.ExportedTypes.Where(type => type.IsClass && !type.IsAbstract).Where(IsHeadlessService))
                .Select(type => new
                {
                    Type = type,
                    ImplementedInterfaces = type.GetInterfaces().Where(HeadlessServiceTypes.Contains).ToArray(),
                    SupportedTargets = type.GetCustomAttribute<SupportedTargetsAttribute>().Keys.ToArray()
                })
                .ToArray();

            // This will register all services by their interface and keyed by their supported target attributes
            // If a service inherits more than 1 recognised interface and/or more than 1 supported target,
            // all requests for any of the matching criteria will return the same instance per DI scope
            foreach (var serviceInfo in serviceInfos)
                foreach (var @interface in serviceInfo.ImplementedInterfaces)
                    foreach (var key in serviceInfo.SupportedTargets)
                        _ = services.Any(s => s.IsKeyedService && s.KeyedImplementationType == serviceInfo.Type) ? services.AddKeyedScoped(@interface, key, (p, _) => p.GetKeyedServices(serviceInfo.ImplementedInterfaces[0], serviceInfo.SupportedTargets[0]).Single()) : services.AddKeyedScoped(@interface, key, serviceInfo.Type);

            return services;
        }
    }
}
