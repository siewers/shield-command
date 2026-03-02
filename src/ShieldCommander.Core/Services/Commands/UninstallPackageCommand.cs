using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class UninstallPackageCommand(string packageName) : IAdbCommand
{
    public string Name => "UninstallPackage";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner)
    {
        return await runner.RunAdbAsync($"uninstall {packageName}");
    }
}
