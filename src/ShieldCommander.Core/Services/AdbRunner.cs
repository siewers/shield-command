using System.Diagnostics;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal sealed class AdbRunner(Func<string> getAdbPath)
{
    private AdbShellSession? _session;

    public async Task OpenSessionAsync(string? deviceSerial = null)
    {
        CloseSession();
        _session = new AdbShellSession(getAdbPath(), deviceSerial);
        await _session.OpenAsync();
    }

    public void CloseSession()
    {
        _session?.Dispose();
        _session = null;
    }

    public static string DeviceArg(string? deviceSerial) =>
        deviceSerial != null ? $"-s {deviceSerial}" : "";

    public static string ShellPrefix(string? deviceSerial) =>
        deviceSerial != null ? $"-s {deviceSerial} shell" : "shell";

    public async Task<string?> RunShellAsync(string command, CancellationToken ct = default)
    {
        if (_session is not null)
        {
            return await _session.RunAsync(command, ct);
        }

        return null;
    }

    public async Task<string> RunShellWithFallbackAsync(string command, string? deviceSerial = null)
    {
        if (_session is not null)
        {
            var output = await RunShellAsync(command);
            if (output is not null)
            {
                return output;
            }
        }

        var prefix = ShellPrefix(deviceSerial);
        var result = await RunAdbAsync($"{prefix} \"{command}\"", strictCheck: false);
        return result.Output;
    }

    public Task<AdbResult> RunAdbAsync(string arguments) => RunAdbAsync(arguments, strictCheck: true);

    public async Task<AdbResult> RunAdbAsync(string arguments, bool strictCheck)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = getAdbPath(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await stdoutTask;
            var error = await stderrTask;

            var success = process.ExitCode == 0;
            if (strictCheck)
            {
                success = success && !output.Contains("error", StringComparison.OrdinalIgnoreCase) && !output.Contains("failed", StringComparison.OrdinalIgnoreCase);
            }

            return new AdbResult(success, output.Trim(), error.Trim());
        }
        catch (Exception ex)
        {
            return new AdbResult(false, "", ex.Message);
        }
    }
}
