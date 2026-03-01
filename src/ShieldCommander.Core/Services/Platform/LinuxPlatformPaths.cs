using System.Diagnostics;

namespace ShieldCommander.Core.Services.Platform;

public sealed class LinuxPlatformPaths : IPlatformPaths
{
    public string AdbExecutableName => "adb";

    public IReadOnlyList<string> AdbSearchPaths { get; } = BuildSearchPaths();

    public string? ResolveExecutablePath(string exe)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"-c \"which {exe}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            var output = process.StandardOutput.ReadLine();
            process.WaitForExit(2000);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) && File.Exists(output))
            {
                return output;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private static List<string> BuildSearchPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return
        [
            Path.Combine(home, "Android", "Sdk", "platform-tools"),
        ];
    }
}
