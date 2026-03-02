using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class TerminateProcessCommand(int pid, string packageName) : IAdbCommand
{
    public string Name => "TerminateProcess";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner, string? deviceSerial)
    {
        var forceStopCmd = $"am force-stop {packageName} 2>&1; echo EXIT:$?";
        var output = (await runner.RunShellWithFallbackAsync(forceStopCmd, deviceSerial)).Trim();
        if (output.Contains("EXIT:0") && !output.Contains("Error"))
        {
            return new AdbResult(true, output);
        }

        var killCmd = $"kill -9 {pid} 2>&1; echo EXIT:$?";
        output = (await runner.RunShellWithFallbackAsync(killCmd, deviceSerial)).Trim();
        var success = output.Contains("EXIT:0");
        var error = output.Replace("EXIT:0", "").Replace("EXIT:1", "").Trim();
        return new AdbResult(success, output, error);
    }
}
