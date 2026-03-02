using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class UninstallPackageCommand(string packageName) : IAdbCommand
{
    public string Name => "UninstallPackage";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner, string? deviceSerial)
    {
        var deviceArg = AdbRunner.DeviceArg(deviceSerial);
        return await runner.RunAdbAsync($"{deviceArg} uninstall {packageName}".Trim());
    }
}
