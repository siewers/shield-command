using Microsoft.Extensions.DependencyInjection;
using ShieldCommander.Core.Services.Platform;

namespace ShieldCommander.UI.Platform;

public static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<IPlatformPaths, MacPlatformPaths>();
            services.AddSingleton<IPlatformShell, MacPlatformShell>();
            services.AddSingleton<IPlatformUI, MacPlatformUI>();
        }
        else if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IPlatformPaths, WindowsPlatformPaths>();
            services.AddSingleton<IPlatformShell, WindowsPlatformShell>();
            services.AddSingleton<IPlatformUI, WindowsPlatformUI>();
        }
        else
        {
            services.AddSingleton<IPlatformPaths, LinuxPlatformPaths>();
            services.AddSingleton<IPlatformShell, LinuxPlatformShell>();
            services.AddSingleton<IPlatformUI, LinuxPlatformUI>();
        }

        return services;
    }
}
