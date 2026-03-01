using System.Diagnostics;
using ShieldCommander.Core.Services.Platform;

namespace ShieldCommander.Core.Services;

public sealed class AdbPathResolver(IPlatformPaths paths)
{
    public string FindAdb()
    {
        var exe = paths.AdbExecutableName;

        var discovered = DiscoverAdbPath(exe);
        if (!string.IsNullOrEmpty(discovered))
        {
            return discovered;
        }

        foreach (var dir in paths.AdbSearchPaths)
        {
            var candidate = Path.Combine(dir, exe);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return exe;
    }

    public static bool CanRunAdb(string adbPath)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = "version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string? DiscoverAdbPath(string exe)
    {
        var resolved = paths.ResolveExecutablePath(exe);
        if (resolved != null)
        {
            return resolved;
        }

        if (CanRunAdb(exe))
        {
            return exe;
        }

        return null;
    }
}
