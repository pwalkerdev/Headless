using Serilog;

namespace Headless.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder
            .ConfigureLogging(lb => { lb.ClearProviders(); })
            .UseSerilog((context, services, config) =>
            {
                config
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services);
            });
    }
}
