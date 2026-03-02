using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class InstallPackageCommand(string apkFilePath) : IAdbCommand
{
    public string Name => "InstallPackage";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner, string? deviceSerial)
    {
        var deviceArg = AdbRunner.DeviceArg(deviceSerial);
        return await runner.RunAdbAsync($"{deviceArg} install -r \"{apkFilePath}\"".Trim());
    }
}
